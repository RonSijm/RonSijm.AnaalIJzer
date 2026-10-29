using System.Collections.Immutable;

namespace RonSijm.IJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionTopologyAnalysisResult(
    ImmutableArray<SolutionTopologyReferenceViolation> referenceViolations,
    ImmutableArray<SolutionTopologyCycle> configuredCycles)
{
    public ImmutableArray<SolutionTopologyReferenceViolation> ReferenceViolations { get; } = referenceViolations;

    public ImmutableArray<SolutionTopologyCycle> ConfiguredCycles { get; } = configuredCycles;
}