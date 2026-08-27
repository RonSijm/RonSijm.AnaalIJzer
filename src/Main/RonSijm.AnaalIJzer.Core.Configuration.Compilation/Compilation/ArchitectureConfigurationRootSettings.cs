using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Exceptions;
using RonSijm.AnaalIJzer.Core.PolicyEvaluation.Config.Model;

namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Compilation;

internal readonly struct ArchitectureConfigurationRootSettings
{
    internal ArchitectureConfigurationRootSettings(
        ImmutableHashSet<string> requiredRecognizedDependencySites,
        ArchitectureExceptionPolicy exceptionPolicy,
        bool enforceAcyclic,
        bool enforceObservedAcyclic,
        OutputConfig output)
    {
        RequiredRecognizedDependencySites = requiredRecognizedDependencySites;
        ExceptionPolicy = exceptionPolicy;
        EnforceAcyclic = enforceAcyclic;
        EnforceObservedAcyclic = enforceObservedAcyclic;
        Output = output;
    }

    internal ImmutableHashSet<string> RequiredRecognizedDependencySites { get; }

    internal ArchitectureExceptionPolicy ExceptionPolicy { get; }

    internal bool EnforceAcyclic { get; }

    internal bool EnforceObservedAcyclic { get; }

    internal OutputConfig Output { get; }
}
