using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Config;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Config.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal static class ApplicationConfigurationGenerator
{
	private const string SchemaResourceName = "RonSijm.AnaalIJzer.Tooling.AnaalIJzer.xsd";
	private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
	private static readonly string[] CommonTypeSuffixes =
	[
		"Controller",
		"Endpoint",
		"Service",
		"Manager",
		"Coordinator",
		"Handler",
		"Repository",
		"Store",
		"Gateway",
		"Client",
		"Queryable",
		"Projection",
		"Factory",
		"Provider",
		"Validator",
		"Mapper",
		"Builder",
		"Options",
		"Configuration"
	];

	public static string Generate(Compilation compilation, string schemaFileName, ConfigurationGenerationOptions options, CancellationToken cancellationToken)
	{
		var types = GetProjectTypes(compilation, cancellationToken);
		if (types.Count == 0)
		{
			throw new ToolingException("The project does not contain any source-defined types to classify.");
		}

		var (layers, typeLayers) = InferLayers(types, compilation.AssemblyName);
		var observedEdges = DiscoverDependencies(compilation, typeLayers, cancellationToken);
		var edges = SelectEdges(observedEdges, options);
		var root = new XElement(
			"ArchitecturalLevels",
			new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
			new XAttribute(Xsi + "noNamespaceSchemaLocation", schemaFileName),
			new XAttribute("description", GetRootDescription(compilation.AssemblyName, options)));

		foreach (var layer in layers)
		{
			var layerElement = new XElement("Layer", new XAttribute("name", layer.Name), new XAttribute("description", layer.Description));
			foreach (var namespacePrefix in layer.NamespacePrefixes)
			{
				var pattern = "^" + Regex.Escape(namespacePrefix) + @"(?:\.|$)";
				var matcher = new XElement("Namespace", new XAttribute("regex", pattern), new XAttribute("description", $"Types in {namespacePrefix} and its child namespaces."));
				AddExceptions(matcher, layer.ExceptionTypes.Where(type => IsInNamespace(type, namespacePrefix)));
				layerElement.Add(matcher);
			}

			foreach (var type in DistinctTypes(layer.ExactTypes))
			{
				var attributeName = type.ContainingType is null ? "exactFullName" : "exactName";
				var matchValue = type.ContainingType is null ? GetAnalyzerFullName(type) : type.Name;
				var matcher = new XElement("Class", new XAttribute(attributeName, matchValue), new XAttribute("description", $"Exact match for {type.ToDisplayString()}."));
				if (layer.ExceptionTypes.Contains(type.OriginalDefinition, SymbolEqualityComparer.Default))
				{
					AddExceptions(matcher, [type]);
				}
				layerElement.Add(matcher);
			}

			root.Add(layerElement);
		}

		foreach (var edge in edges)
		{
			var sites = string.Join(", ", DependencySites.All.Where(edge.Sites.Contains));
			root.Add(new XElement(
				"AllowedDependency",
				new XAttribute("from", edge.From.Name),
				new XAttribute("to", edge.To.Name),
				new XAttribute("allowedSites", sites),
				new XAttribute("description", GetEdgeDescription(edge, sites))));
		}

		return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + root + Environment.NewLine;
	}

	private static string GetRootDescription(string? assemblyName, ConfigurationGenerationOptions options) =>
		options.Strategy == ConfigurationGenerationStrategy.Snapshot
			? $"Generated snapshot architecture for {assemblyName ?? "the project"}. Review inferred layers and observed dependency edges before adopting it."
			: $"Generated convention architecture for {assemblyName ?? "the project"} using minimum confidence {options.MinimumConfidence * 100:F0}% and minimum support {options.MinimumSupport}. Minority dependencies are grandfathered as layer matcher exceptions.";

	private static string GetEdgeDescription(GeneratedEdge edge, string sites) => edge.Disposition switch
	{
		EdgeDisposition.Convention => $"Inferred convention: {edge.CallerCount} of {edge.ActiveCallerCount} active {edge.From.Name} callers ({(double)edge.CallerCount / edge.ActiveCallerCount * 100:F0}%) depend on {edge.To.Name}. Observed sites: {sites}.",
		EdgeDisposition.AmbiguousSnapshot => $"Observed {edge.CallerCount} of {edge.ActiveCallerCount} active {edge.From.Name} callers depending on {edge.To.Name} at: {sites}. Preserved because this layer had no dependency edge with enough confidence and support to establish a convention.",
		_ => $"Observed {edge.CallerCount} {edge.From.Name} caller(s) depending on {edge.To.Name} at: {sites}."
	};

	private static void AddExceptions(XElement matcher, IEnumerable<INamedTypeSymbol> exceptionTypes)
	{
		var types = DistinctTypes(exceptionTypes).OrderBy(GetAnalyzerFullName, StringComparer.Ordinal).ToArray();
		if (types.Length == 0)
		{
			return;
		}

		var exceptions = new XElement("Exceptions", new XAttribute("description", "Generated exceptions for callers that do not follow an inferred dependency convention."));
		foreach (var type in types)
		{
			exceptions.Add(new XElement(
				"Class",
				new XAttribute("exactFullName", GetAnalyzerFullName(type)),
				new XAttribute("description", $"Grandfathers the observed dependencies of {type.ToDisplayString()}.")));
		}
		matcher.Add(exceptions);
	}

	private static bool IsInNamespace(INamedTypeSymbol type, string namespacePrefix)
	{
		var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
		return namespaceName == namespacePrefix || namespaceName.StartsWith(namespacePrefix + ".", StringComparison.Ordinal);
	}

	public static string ReadSchema()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SchemaResourceName)
			?? throw new InvalidOperationException($"Embedded resource not found: {SchemaResourceName}");
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public static Task<ImmutableArray<Diagnostic>> ValidateAsync(Compilation compilation, string configuration, string configurationPath, CancellationToken cancellationToken)
	{
		var options = new AnalyzerOptions([new GeneratedAdditionalText(configurationPath, configuration)]);
		return compilation.WithAnalyzers([new ArchitecturalLevelAnalyzer()], options).GetAnalyzerDiagnosticsAsync(cancellationToken);
	}

	public static AnalyzerConfiguration Parse(Compilation compilation, string configuration, string configurationPath, CancellationToken cancellationToken)
	{
		var additionalText = new GeneratedAdditionalText(configurationPath, configuration);
		return ArchitecturalConfigParser.Parse([additionalText], compilation, configurationPath, cancellationToken);
	}

	internal static IReadOnlyList<INamedTypeSymbol> GetProjectTypes(Compilation compilation, CancellationToken cancellationToken)
	{
		var types = new List<INamedTypeSymbol>();
		CollectNamespaceTypes(compilation.Assembly.GlobalNamespace, types, cancellationToken);
		return DistinctTypes(types)
			.OrderBy(type => type.Locations.FirstOrDefault(location => location.IsInSource)?.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(type => type.Locations.FirstOrDefault(location => location.IsInSource)?.SourceSpan.Start ?? int.MaxValue)
			.ToArray();
	}

	private static void CollectNamespaceTypes(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> types, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		foreach (var type in namespaceSymbol.GetTypeMembers())
		{
			CollectType(type, types, cancellationToken);
		}

		foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
		{
			CollectNamespaceTypes(childNamespace, types, cancellationToken);
		}
	}

	private static IEnumerable<INamedTypeSymbol> DistinctTypes(IEnumerable<INamedTypeSymbol> types)
	{
		var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
		foreach (var type in types)
		{
			if (seen.Add(type))
			{
				yield return type;
			}
		}
	}

	private static void CollectType(INamedTypeSymbol type, List<INamedTypeSymbol> types, CancellationToken cancellationToken)
	{
		if (type.DeclaringSyntaxReferences.Any(reference => !IsGenerated(reference.SyntaxTree, cancellationToken)))
		{
			types.Add(type.OriginalDefinition);
		}

		foreach (var nestedType in type.GetTypeMembers())
		{
			CollectType(nestedType, types, cancellationToken);
		}
	}

	internal static bool IsGenerated(SyntaxTree syntaxTree, CancellationToken cancellationToken)
	{
		var filePath = syntaxTree.FilePath;
		if (!string.IsNullOrWhiteSpace(filePath))
		{
			var normalizedPath = filePath.Replace('/', '\\');
			var fileName = Path.GetFileName(filePath);
			if (normalizedPath.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)
			    || fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
			    || fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		var text = syntaxTree.GetText(cancellationToken);
		var prefixLength = Math.Min(text.Length, 512);
		return text.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(0, prefixLength)).Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
	}

	private static (IReadOnlyList<GeneratedLayer> Layers, Dictionary<INamedTypeSymbol, GeneratedLayer> TypeLayers) InferLayers(IReadOnlyList<INamedTypeSymbol> types, string? assemblyName)
	{
		var baseNamespace = FindBaseNamespace(types, assemblyName);
		var layers = new List<GeneratedLayer>();
		var layersByName = new Dictionary<string, GeneratedLayer>(StringComparer.Ordinal);
		var typeLayers = new Dictionary<INamedTypeSymbol, GeneratedLayer>(SymbolEqualityComparer.Default);

		foreach (var type in types)
		{
			var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
			var hasNamespaceLayer = TryGetNamespaceLayer(namespaceName, baseNamespace, out var layerName, out var namespacePrefix);
			if (!hasNamespaceLayer)
			{
				layerName = GetTypeLayerName(type.Name);
			}

			if (!layersByName.TryGetValue(layerName, out var layer))
			{
				layer = new GeneratedLayer(layerName, $"Inferred {layerName} layer from the project's namespaces and type names.");
				layersByName.Add(layerName, layer);
				layers.Add(layer);
			}

			if (hasNamespaceLayer)
			{
				if (!layer.NamespacePrefixes.Contains(namespacePrefix, StringComparer.Ordinal))
				{
					layer.NamespacePrefixes.Add(namespacePrefix);
				}
			}
			else
			{
				layer.ExactTypes.Add(type);
			}

			typeLayers[type.OriginalDefinition] = layer;
		}

		return (layers, typeLayers);
	}

	private static string FindBaseNamespace(IReadOnlyList<INamedTypeSymbol> types, string? assemblyName)
	{
		var namespaces = types
			.Select(type => type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString())
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		if (namespaces.Length == 0)
		{
			return string.Empty;
		}

		if (!string.IsNullOrWhiteSpace(assemblyName)
		    && namespaces.All(value => value == assemblyName || value.StartsWith(assemblyName + ".", StringComparison.Ordinal)))
		{
			return assemblyName;
		}

		if (namespaces.Length == 1)
		{
			var separator = namespaces[0].LastIndexOf('.');
			return separator < 0 ? string.Empty : namespaces[0][..separator];
		}

		var segments = namespaces.Select(value => value.Split('.')).ToArray();
		var commonLength = 0;
		while (segments.All(parts => parts.Length > commonLength && parts[commonLength] == segments[0][commonLength]))
		{
			commonLength++;
		}

		return string.Join(".", segments[0].Take(commonLength));
	}

	private static bool TryGetNamespaceLayer(string namespaceName, string baseNamespace, out string layerName, out string namespacePrefix)
	{
		var remainder = namespaceName;
		if (baseNamespace.Length > 0)
		{
			if (namespaceName == baseNamespace)
			{
				layerName = string.Empty;
				namespacePrefix = string.Empty;
				return false;
			}

			if (namespaceName.StartsWith(baseNamespace + ".", StringComparison.Ordinal))
			{
				remainder = namespaceName[(baseNamespace.Length + 1)..];
			}
		}

		if (remainder.Length == 0)
		{
			layerName = string.Empty;
			namespacePrefix = string.Empty;
			return false;
		}

		layerName = remainder.Split('.')[0];
		namespacePrefix = baseNamespace.Length > 0 ? baseNamespace + "." + layerName : layerName;
		return true;
	}

	private static string GetTypeLayerName(string typeName)
	{
		foreach (var suffix in CommonTypeSuffixes)
		{
			if (typeName.EndsWith(suffix, StringComparison.Ordinal))
			{
				return suffix;
			}
		}

		return "Core";
	}

	private static IReadOnlyList<GeneratedEdge> DiscoverDependencies(Compilation compilation, IReadOnlyDictionary<INamedTypeSymbol, GeneratedLayer> typeLayers, CancellationToken cancellationToken)
	{
		var edges = new List<GeneratedEdge>();
		var edgesByLayers = new Dictionary<(GeneratedLayer From, GeneratedLayer To), GeneratedEdge>();
		string? ResolveLayer(INamedTypeSymbol type) =>
			typeLayers.TryGetValue(type.OriginalDefinition, out var layer) ? layer.Name : null;

		foreach (var observation in ProjectDependencyScanner.Scan(compilation, ResolveLayer, cancellationToken))
		{
			if (!typeLayers.TryGetValue(observation.CallerType, out var callerLayer)
			    || !typeLayers.TryGetValue(observation.DependencyType, out var dependencyLayer))
			{
				continue;
			}

			var key = (callerLayer, dependencyLayer);
			if (!edgesByLayers.TryGetValue(key, out var edge))
			{
				edge = new GeneratedEdge(callerLayer, dependencyLayer);
				edgesByLayers.Add(key, edge);
				edges.Add(edge);
			}

			edge.AddObservation(observation.CallerType, observation.Site);
		}

		return edges;
	}

	private static IReadOnlyList<GeneratedEdge> SelectEdges(IReadOnlyList<GeneratedEdge> observedEdges, ConfigurationGenerationOptions options)
	{
		if (options.Strategy == ConfigurationGenerationStrategy.Snapshot)
		{
			return observedEdges;
		}

		var selectedEdges = new List<GeneratedEdge>();
		foreach (var layerEdges in observedEdges.GroupBy(edge => edge.From))
		{
			var edges = layerEdges.ToArray();
			var activeCallers = DistinctTypes(edges.SelectMany(edge => edge.Callers)).ToArray();
			foreach (var edge in edges)
			{
				edge.ActiveCallerCount = activeCallers.Length;
			}
			var conventionalEdges = edges
				.Where(edge => edge.CallerCount >= options.MinimumSupport
				               && (double)edge.CallerCount / activeCallers.Length >= options.MinimumConfidence)
				.ToArray();

			if (conventionalEdges.Length == 0)
			{
				foreach (var edge in edges)
				{
					edge.Disposition = EdgeDisposition.AmbiguousSnapshot;
				}
				selectedEdges.AddRange(edges);
				continue;
			}

			foreach (var edge in conventionalEdges)
			{
				edge.Disposition = EdgeDisposition.Convention;
			}
			selectedEdges.AddRange(conventionalEdges);
			var rejectedEdges = edges.Except(conventionalEdges);
			foreach (var caller in DistinctTypes(rejectedEdges.SelectMany(edge => edge.Callers)))
			{
				layerEdges.Key.ExceptionTypes.Add(caller);
			}
		}

		return selectedEdges;
	}

	private static string GetAnalyzerFullName(INamedTypeSymbol type)
	{
		var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
		return namespaceName.Length == 0 ? type.Name : namespaceName + "." + type.Name;
	}

	private sealed class GeneratedLayer
	{
		public GeneratedLayer(string name, string description)
		{
			Name = name;
			Description = description;
		}

		public string Name { get; }
		public string Description { get; }
		public List<string> NamespacePrefixes { get; } = [];
		public List<INamedTypeSymbol> ExactTypes { get; } = [];
		public HashSet<INamedTypeSymbol> ExceptionTypes { get; } = new(SymbolEqualityComparer.Default);
	}

	private sealed class GeneratedEdge
	{
		public GeneratedEdge(GeneratedLayer from, GeneratedLayer to)
		{
			From = from;
			To = to;
		}

		public GeneratedLayer From { get; }
		public GeneratedLayer To { get; }
		public HashSet<string> Sites { get; } = new(StringComparer.Ordinal);
		public HashSet<INamedTypeSymbol> Callers { get; } = new(SymbolEqualityComparer.Default);
		public int CallerCount => Callers.Count;
		public int ActiveCallerCount { get; set; }
		public EdgeDisposition Disposition { get; set; }

		public void AddObservation(INamedTypeSymbol caller, string site)
		{
			Callers.Add(caller);
			Sites.Add(site);
		}
	}

	private enum EdgeDisposition
	{
		Snapshot,
		Convention,
		AmbiguousSnapshot
	}

	private sealed class GeneratedAdditionalText : AdditionalText
	{
		private readonly SourceText _text;

		public GeneratedAdditionalText(string path, string content)
		{
			Path = path;
			_text = SourceText.From(content);
		}

		public override string Path { get; }
		public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
	}
}
