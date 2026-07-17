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
	private static IReadOnlyList<GeneratedEdge> DiscoverDependencies(Compilation compilation, IReadOnlyDictionary<INamedTypeSymbol, GeneratedLayer> typeLayers, CancellationToken cancellationToken)
	{
		var edges = new List<GeneratedEdge>();
		var edgesByLayers = new Dictionary<(GeneratedLayer From, GeneratedLayer To), GeneratedEdge>();
		string? ResolveLayer(INamedTypeSymbol type)
		{
			var result = typeLayers.TryGetValue(type.OriginalDefinition, out var layer) ? layer.Name : null;

			return result;
		}

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
		if (options.Strategy is ConfigurationGenerationStrategy.Snapshot or ConfigurationGenerationStrategy.Helpful)
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
		var result = namespaceName.Length == 0 ? type.Name : namespaceName + "." + type.Name;

		return result;
	}

	private sealed class GeneratedLayer(string name, string description)
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public List<string> AssemblyNames { get; } = [];
        public List<string> NamespacePrefixes { get; } = [];
		public List<INamedTypeSymbol> ExactTypes { get; } = [];
		public HashSet<INamedTypeSymbol> ExceptionTypes { get; } = new(SymbolEqualityComparer.Default);
	}

	private sealed class GeneratedEdge(GeneratedLayer from, GeneratedLayer to)
    {
        public GeneratedLayer From { get; } = from;
        public GeneratedLayer To { get; } = to;
        public HashSet<string> Sites { get; } = new(StringComparer.Ordinal);
		public HashSet<INamedTypeSymbol> Callers { get; } = new(SymbolEqualityComparer.Default);
		public int CallerCount
        {
            get { return Callers.Count; }
        }

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

	private sealed class GeneratedAdditionalText(string path, string content) : AdditionalText
    {
		private readonly SourceText _text = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = _text;

			return result;
		}
	}
}
