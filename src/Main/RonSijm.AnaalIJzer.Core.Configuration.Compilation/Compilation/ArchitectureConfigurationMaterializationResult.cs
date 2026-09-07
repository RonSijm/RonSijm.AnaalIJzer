using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;
using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Compilation;

internal readonly struct ArchitectureConfigurationMaterializationResult
{
    internal ArchitectureConfigurationMaterializationResult(
        CompiledLayerCatalog layerCatalog,
        ImmutableArray<DependencyEdge> dependencyEdges,
        ImmutableDictionary<string, ImmutableHashSet<string>> layerRequiredRecognizedDependencySites,
        ImmutableArray<ArchitectureExceptionDefinition> exceptionDefinitions,
        ImmutableArray<ArchitectureExceptionReview> exceptionReviews,
        ImmutableArray<string> layerNames,
        ImmutableArray<ArchitectureForbiddenPattern> forbiddenPatterns,
        ProjectArchitectureConfig projectArchitecture,
        SolutionTopologyConfig solutionTopology = default,
        OperationContractCatalog operationContracts = default,
        AssemblyAttributePolicyCatalog assemblyAttributePolicies = default)
    {
        LayerCatalog = layerCatalog;
        DependencyEdges = dependencyEdges;
        LayerRequiredRecognizedDependencySites = layerRequiredRecognizedDependencySites;
        ExceptionDefinitions = exceptionDefinitions;
        ExceptionReviews = exceptionReviews;
        LayerNames = layerNames;
        ForbiddenPatterns = forbiddenPatterns;
        ProjectArchitecture = projectArchitecture;
        SolutionTopology = solutionTopology;
        OperationContracts = operationContracts;
        AssemblyAttributePolicies = assemblyAttributePolicies;
    }

    internal CompiledLayerCatalog LayerCatalog { get; }

    internal ImmutableArray<DependencyEdge> DependencyEdges { get; }

    internal ImmutableDictionary<string, ImmutableHashSet<string>> LayerRequiredRecognizedDependencySites { get; }

    internal ImmutableArray<ArchitectureExceptionDefinition> ExceptionDefinitions { get; }

    internal ImmutableArray<ArchitectureExceptionReview> ExceptionReviews { get; }

    internal ImmutableArray<string> LayerNames { get; }

    internal ImmutableArray<ArchitectureForbiddenPattern> ForbiddenPatterns { get; }

    internal ProjectArchitectureConfig ProjectArchitecture { get; }

    internal SolutionTopologyConfig SolutionTopology { get; }

    internal OperationContractCatalog OperationContracts { get; }

    internal AssemblyAttributePolicyCatalog AssemblyAttributePolicies { get; }
}
