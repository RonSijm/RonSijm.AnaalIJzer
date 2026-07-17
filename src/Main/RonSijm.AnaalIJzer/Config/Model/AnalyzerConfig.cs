using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Definitions;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Parsing;

namespace RonSijm.AnaalIJzer.Model;

/// <summary>
///     Aggregate root for the fully-parsed analyzer configuration.
///     Delegates matching to <see cref="LayerRegistry" />, edge rules to
///     <see cref="DependencyGraph" />, and output settings to <see cref="OutputConfig" />.
/// </summary>
internal readonly struct AnalyzerConfig(
	LayerRegistry registry,
	DependencyGraph graph,
	OutputConfig output,
	ImmutableHashSet<string> requiredRecognizedDependencySites,
	ImmutableDictionary<string, ImmutableHashSet<string>> layerRequiredRecognizedDependencySites,
	bool enforceAcyclic,
	ImmutableArray<LayerNode> layers,
	ImmutableArray<string> layerNames,
	ImmutableArray<(string Name, string? Comment)> forbiddenPatterns,
	ArchitectureDocumentation documentation,
	ImmutableArray<ConfigurationIssue> configurationIssues)
{
	public static readonly AnalyzerConfig Empty = new(new LayerRegistry(ImmutableArray<LayerNode>.Empty, ImmutableDictionary<string, LayerNode>.Empty, ImmutableDictionary<string, MatcherRule>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty), new DependencyGraph(ImmutableArray<DependencyEdge>.Empty), new OutputConfig(false, string.Empty, false, string.Empty), ImmutableHashSet<string>.Empty, ImmutableDictionary<string, ImmutableHashSet<string>>.Empty, false, ImmutableArray<LayerNode>.Empty, ImmutableArray<string>.Empty, ImmutableArray<(string, string?)>.Empty, ArchitectureDocumentation.Empty, ImmutableArray<ConfigurationIssue>.Empty);

	internal static AnalyzerConfig Invalid(ConfigurationIssue issue)
	{
		var result = new AnalyzerConfig(new LayerRegistry(ImmutableArray<LayerNode>.Empty, ImmutableDictionary<string, LayerNode>.Empty, ImmutableDictionary<string, MatcherRule>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty, ImmutableArray<(PatternMatcher, MatcherRule)>.Empty), new DependencyGraph(ImmutableArray<DependencyEdge>.Empty), new OutputConfig(false, string.Empty, false, string.Empty), ImmutableHashSet<string>.Empty, ImmutableDictionary<string, ImmutableHashSet<string>>.Empty, false, ImmutableArray<LayerNode>.Empty, ImmutableArray<string>.Empty, ImmutableArray<(string, string?)>.Empty, ArchitectureDocumentation.Empty, [issue]);

		return result;
	}

	/// <summary>The dependency edge rules (allowed edges, wildcards, all-allowed flag).</summary>
	public DependencyGraph Graph { get; } = graph;

	/// <summary>The opt-in output settings (report and documentation paths).</summary>
	public OutputConfig Output { get; } = output;

	public ArchitectureDocumentation Documentation { get; } = documentation;

	public ImmutableHashSet<string> RequiredRecognizedDependencySites { get; } = requiredRecognizedDependencySites;
	public ImmutableDictionary<string, ImmutableHashSet<string>> LayerRequiredRecognizedDependencySites { get; } = layerRequiredRecognizedDependencySites;
	public bool EnforceAcyclic { get; } = enforceAcyclic;
	public ImmutableArray<ConfigurationIssue> ConfigurationIssues { get; } = configurationIssues;

	/// <summary>
	///     Names of all declared <c>&lt;Layer&gt;</c> elements in document order.
	///     Used by the documentation generator to render the architecture graph.
	/// </summary>
	public ImmutableArray<string> LayerNames { get; } = layerNames;

	/// <summary>The configured layer hierarchy in document order.</summary>
	public ImmutableArray<LayerNode> Layers { get; } = layers;

	/// <summary>
	///     Forbidden patterns (display name + optional comment) in document order.
	///     Used by the documentation generator to render forbidden pattern summaries.
	/// </summary>
	public ImmutableArray<(string Name, string? Comment)> ForbiddenPatterns { get; } = forbiddenPatterns;

	// Delegating properties — keep the flat surface area that callers and tests already use.
	public ImmutableHashSet<(string From, string To)> AllowedEdges => Graph.AllowedEdges;
	public ImmutableHashSet<string> WildcardTargets => Graph.WildcardTargets;
	public ImmutableHashSet<string> WildcardSources => Graph.WildcardSources;
	public bool AllowAnyDependency => Graph.AllowAnyDependency;
	public bool EnableReport => Output.EnableReport;
	public string ReportPath => Output.ReportPath;
	public bool EnableDocumentation => Output.EnableDocumentation;
	public string DocumentationPath => Output.DocumentationPath;

	public bool HasLayers => registry.HasLayers;
	public bool HasConfigurationIssues => !ConfigurationIssues.IsDefaultOrEmpty;
	public bool RequiresRecognizedDependencyAt(string site)
	{
		var result = RequiredRecognizedDependencySites.Contains(site);

		return result;
	}
	public bool RequiresRecognizedDependencyAt(LayerMatch callerMatch, string site)
	{
		if (RequiresRecognizedDependencyAt(site))
		{
			return true;
		}

		foreach (var layer in callerMatch.Layers)
		{
			if (LayerRequiredRecognizedDependencySites.TryGetValue(layer.Name, out var sites) && sites.Contains(site))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Finds the layer for a type. Delegates to <see cref="LayerRegistry.FindLayer" />.
	/// </summary>
	public LayerMatch? FindLayer(string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = registry.FindLayer(typeName, namespaceName, symbol);

		return result;
	}

	public TypePolicyViolation? EvaluateTypePolicy(LayerMatch layerMatch, string typeName, string namespaceName, ITypeSymbol? symbol = null)
	{
		var result = registry.EvaluateTypePolicy(layerMatch, typeName, namespaceName, symbol);

		return result;
	}
}
