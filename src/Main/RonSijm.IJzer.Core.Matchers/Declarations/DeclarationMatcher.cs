using System.Collections.Immutable;
using RonSijm.IJzer.Core.Matchers.Conditions;
using RonSijm.IJzer.Core.Matchers.Observations;

namespace RonSijm.IJzer.Core.Matchers.Declarations;

public readonly struct DeclarationMatcher(
    DeclarationMatchTarget target,
    ImmutableArray<MatchCondition> conditions,
    ImmutableArray<CodeObservationMatcher> requiredObservations = default)
{
    public DeclarationMatchTarget Target { get; } = target;

    public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

    public ImmutableArray<CodeObservationMatcher> RequiredObservations { get; } = requiredObservations;
}