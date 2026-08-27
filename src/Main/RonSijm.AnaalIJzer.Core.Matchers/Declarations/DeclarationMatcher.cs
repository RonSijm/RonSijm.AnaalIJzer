using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Matchers.Declarations;

public readonly struct DeclarationMatcher(
	DeclarationMatchTarget target,
	ImmutableArray<MatchCondition> conditions,
	ImmutableArray<CodeObservationMatcher> requiredObservations = default)
{
	public DeclarationMatchTarget Target { get; } = target;

	public ImmutableArray<MatchCondition> Conditions { get; } = conditions;

	public ImmutableArray<CodeObservationMatcher> RequiredObservations { get; } = requiredObservations;
}
