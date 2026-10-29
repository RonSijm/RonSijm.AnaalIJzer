using System.Collections.Immutable;
using RonSijm.IJzer.Core.AssemblyAttributes.Model;
using RonSijm.IJzer.Core.Configuration.Document.Model;
using RonSijm.IJzer.Core.DependencyRules;
using RonSijm.IJzer.Core.Exceptions;
using RonSijm.IJzer.Core.LayerModel;
using RonSijm.IJzer.Core.Observations;
using RonSijm.IJzer.Core.NamespaceHierarchy.Policies;
using RonSijm.IJzer.Core.OperationContracts.Model;
using RonSijm.IJzer.Core.PolicyEvaluation.Config.Model;
using RonSijm.IJzer.Core.PolicyEvaluation.Engine.DependencyRules;
using RonSijm.IJzer.Core.ProjectArchitecture;
using RonSijm.IJzer.Core.ProjectArchitecture.SolutionTopology;
using RonSijm.IJzer.Core.ReturnValues.Policies;

namespace RonSijm.IJzer.Core.PolicyEvaluation.Engine.Policies;

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
    ImmutableArray<ConfigurationIssue> configurationIssues,
    SolutionTopologyConfig solutionTopology = default,
    GeneratedCodeAnalysisScope generatedCodeScope = default,
    OperationContractCatalog operationContracts = default,
    AssemblyAttributePolicyCatalog assemblyAttributePolicies = default,
    ImmutableArray<ReturnValuePolicy> globalReturnValuePolicies = default,
    ImmutableArray<NamespaceHierarchyPolicy> namespaceHierarchyPolicies = default)
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
        ImmutableArray<ConfigurationIssue>.Empty,
        SolutionTopologyConfig.Empty);

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
    public SolutionTopologyConfig SolutionTopology { get; } = solutionTopology;
    public GeneratedCodeAnalysisScope GeneratedCodeScope { get; } = generatedCodeScope;
    public OperationContractCatalog OperationContracts { get; } = operationContracts;
    public AssemblyAttributePolicyCatalog AssemblyAttributePolicies { get; } = assemblyAttributePolicies;
    public ImmutableArray<ReturnValuePolicy> GlobalReturnValuePolicies { get; } = globalReturnValuePolicies.IsDefault ? ImmutableArray<ReturnValuePolicy>.Empty : globalReturnValuePolicies;
    public ImmutableArray<NamespaceHierarchyPolicy> NamespaceHierarchyPolicies { get; } = namespaceHierarchyPolicies.IsDefault ? ImmutableArray<NamespaceHierarchyPolicy>.Empty : namespaceHierarchyPolicies;
    public ArchitectureDocumentation Documentation { get; } = documentation;
    public ImmutableArray<ConfigurationIssue> ConfigurationIssues { get; } = configurationIssues;
}