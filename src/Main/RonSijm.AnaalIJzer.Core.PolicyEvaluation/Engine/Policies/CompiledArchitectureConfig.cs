using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Config.Model;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.Policies;

public readonly struct CompiledArchitectureConfig(
	CompiledLayerCatalog layerCatalog,
	DependencyGraph graph,
	OutputConfig output,
	ImmutableHashSet<string> requiredRecognizedDependencySites,
	ImmutableDictionary<string, ImmutableHashSet<string>> layerRequiredRecognizedDependencySites,
	ArchitectureExceptionPolicy exceptionPolicy,
	ImmutableArray<ArchitectureExceptionDefinition> exceptionDefinitions,
	ImmutableArray<ArchitectureExceptionReview> exceptionReviews,
	bool enforceAcyclic,
	bool enforceObservedAcyclic,
	ImmutableArray<string> layerNames,
	ImmutableArray<(string Name, string? Comment)> forbiddenPatterns,
	ProjectArchitectureConfig projectArchitecture,
	ArchitectureDocumentation documentation,
	ImmutableArray<ConfigurationIssue> configurationIssues)
{
	public static readonly CompiledArchitectureConfig Empty = new(
		CompiledLayerCatalog.Empty,
		new DependencyGraph(ImmutableArray<DependencyEdge>.Empty),
		new OutputConfig(false, string.Empty, false, string.Empty),
		ImmutableHashSet<string>.Empty,
		ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
		ArchitectureExceptionPolicy.Disabled,
		ImmutableArray<ArchitectureExceptionDefinition>.Empty,
		ImmutableArray<ArchitectureExceptionReview>.Empty,
		false,
		false,
		ImmutableArray<string>.Empty,
		ImmutableArray<(string, string?)>.Empty,
		ProjectArchitectureConfig.Empty,
		ArchitectureDocumentation.Empty,
		ImmutableArray<ConfigurationIssue>.Empty);

	public CompiledLayerCatalog LayerCatalog { get; } = layerCatalog;
	public DependencyGraph Graph { get; } = graph;
	public OutputConfig Output { get; } = output;
	public ImmutableHashSet<string> RequiredRecognizedDependencySites { get; } = requiredRecognizedDependencySites;
	public ImmutableDictionary<string, ImmutableHashSet<string>> LayerRequiredRecognizedDependencySites { get; } = layerRequiredRecognizedDependencySites;
	public ArchitectureExceptionPolicy ExceptionPolicy { get; } = exceptionPolicy;
	public ImmutableArray<ArchitectureExceptionDefinition> ExceptionDefinitions { get; } = exceptionDefinitions;
	public ImmutableArray<ArchitectureExceptionReview> ExceptionReviews { get; } = exceptionReviews;
	public bool EnforceAcyclic { get; } = enforceAcyclic;
	public bool EnforceObservedAcyclic { get; } = enforceObservedAcyclic;
	public ImmutableArray<string> LayerNames { get; } = layerNames;
	public ImmutableArray<(string Name, string? Comment)> ForbiddenPatterns { get; } = forbiddenPatterns;
	public ProjectArchitectureConfig ProjectArchitecture { get; } = projectArchitecture;
	public ArchitectureDocumentation Documentation { get; } = documentation;
	public ImmutableArray<ConfigurationIssue> ConfigurationIssues { get; } = configurationIssues;
}
