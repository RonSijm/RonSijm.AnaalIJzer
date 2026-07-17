using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Tooling;

internal static partial class ApplicationConfigurationGenerator
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
			foreach (var assemblyName in layer.AssemblyNames)
			{
				var matcher = new XElement("Assembly", new XAttribute("exactName", assemblyName), new XAttribute("description", $"Types compiled into {assemblyName}."));
				AddExceptions(matcher, layer.ExceptionTypes.Where(type => string.Equals(type.ContainingAssembly?.Name, assemblyName, StringComparison.Ordinal)));
				layerElement.Add(matcher);
			}

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

	public static string Generate(SolutionAnalysisResult solution, string schemaFileName, ConfigurationGenerationOptions options, CancellationToken cancellationToken)
	{
		var (layers, layersByAssemblyName) = InferSolutionLayers(solution, cancellationToken);
		if (layers.Count == 0)
		{
			throw new ToolingException("The solution does not contain any C# projects with source-defined types to classify.");
		}

		var observedEdges = DiscoverSolutionDependencies(solution, layersByAssemblyName, cancellationToken);
		var edges = SelectEdges(observedEdges, options);
		var root = new XElement(
			"ArchitecturalLevels",
			new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
			new XAttribute(Xsi + "noNamespaceSchemaLocation", schemaFileName),
			new XAttribute("description", GetSolutionRootDescription(solution.SolutionName, options)));

		foreach (var layer in layers)
		{
			var layerElement = new XElement("Layer", new XAttribute("name", layer.Name), new XAttribute("description", layer.Description));
			foreach (var assemblyName in layer.AssemblyNames)
			{
				var matcher = new XElement("Assembly", new XAttribute("exactName", assemblyName), new XAttribute("description", $"Types compiled into {assemblyName}."));
				AddExceptions(matcher, layer.ExceptionTypes.Where(type => string.Equals(type.ContainingAssembly?.Name, assemblyName, StringComparison.Ordinal)));
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

	private static string GetRootDescription(string? assemblyName, ConfigurationGenerationOptions options)
	{
		var result = options.Strategy switch
		{
			ConfigurationGenerationStrategy.Conventions => $"Generated convention architecture for {assemblyName ?? "the project"} using minimum confidence {options.MinimumConfidence * 100:F0}% and minimum support {options.MinimumSupport}. Minority dependencies are grandfathered as layer matcher exceptions.",
			ConfigurationGenerationStrategy.Helpful => $"Generated helpful baseline architecture for {assemblyName ?? "the project"}. Review inferred layers and observed dependency edges before tightening it.",
			_ => $"Generated snapshot architecture for {assemblyName ?? "the project"}. Review inferred layers and observed dependency edges before adopting it."
		};

		return result;
	}

	private static string GetSolutionRootDescription(string solutionName, ConfigurationGenerationOptions options)
	{
		var result = options.Strategy switch
		{
			ConfigurationGenerationStrategy.Conventions => $"Generated convention architecture for solution {solutionName} using minimum confidence {options.MinimumConfidence * 100:F0}% and minimum support {options.MinimumSupport}. Minority dependencies are grandfathered as layer matcher exceptions.",
			ConfigurationGenerationStrategy.Helpful => $"Generated helpful solution baseline for {solutionName}. Each C# project is represented as an assembly layer, with observed inter-project dependency sites allowed.",
			_ => $"Generated solution snapshot for {solutionName}. Each C# project is represented as an assembly layer, with observed dependency sites allowed."
		};

		return result;
	}

	private static string GetEdgeDescription(GeneratedEdge edge, string sites)
	{
		var result = edge.Disposition switch
		{
			EdgeDisposition.Convention =>
				$"Inferred convention: {edge.CallerCount} of {edge.ActiveCallerCount} active {edge.From.Name} callers ({(double)edge.CallerCount / edge.ActiveCallerCount * 100:F0}%) depend on {edge.To.Name}. Observed sites: {sites}.",
			EdgeDisposition.AmbiguousSnapshot =>
				$"Observed {edge.CallerCount} of {edge.ActiveCallerCount} active {edge.From.Name} callers depending on {edge.To.Name} at: {sites}. Preserved because this layer had no dependency edge with enough confidence and support to establish a convention.",
			_ => $"Observed {edge.CallerCount} {edge.From.Name} caller(s) depending on {edge.To.Name} at: {sites}."
		};

		return result;
	}

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
		var result = namespaceName == namespacePrefix || namespaceName.StartsWith(namespacePrefix + ".", StringComparison.Ordinal);

		return result;
	}

}
