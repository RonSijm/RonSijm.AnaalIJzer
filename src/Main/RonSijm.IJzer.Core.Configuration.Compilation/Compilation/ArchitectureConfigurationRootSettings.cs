using System.Collections.Immutable;
using RonSijm.IJzer.Core.Exceptions;
using RonSijm.IJzer.Core.Observations;
using RonSijm.IJzer.Core.PolicyEvaluation.Config.Model;

namespace RonSijm.IJzer.Core.Configuration.Compilation.Compilation;

internal readonly struct ArchitectureConfigurationRootSettings
{
    internal ArchitectureConfigurationRootSettings(
        ImmutableHashSet<string> requiredRecognizedDependencySites,
        ArchitectureExceptionPolicy exceptionPolicy,
        bool enforceAcyclic,
        bool enforceObservedAcyclic,
        OutputConfig output,
        GeneratedCodeAnalysisScope generatedCodeScope = default)
    {
        RequiredRecognizedDependencySites = requiredRecognizedDependencySites;
        ExceptionPolicy = exceptionPolicy;
        EnforceAcyclic = enforceAcyclic;
        EnforceObservedAcyclic = enforceObservedAcyclic;
        Output = output;
        GeneratedCodeScope = generatedCodeScope;
    }

    internal ImmutableHashSet<string> RequiredRecognizedDependencySites { get; }

    internal ArchitectureExceptionPolicy ExceptionPolicy { get; }

    internal bool EnforceAcyclic { get; }

    internal bool EnforceObservedAcyclic { get; }

    internal OutputConfig Output { get; }

    internal GeneratedCodeAnalysisScope GeneratedCodeScope { get; }
}