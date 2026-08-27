using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Inheritance.Policies;

public readonly struct InheritancePolicyEvaluation(
	InheritancePolicy policy,
	InheritanceViolationKind violationKind,
	string reason,
	ImmutableArray<string> missingTypeNames)
{
	public InheritancePolicy Policy { get; } = policy;

	public InheritanceViolationKind ViolationKind { get; } = violationKind;

	public string Reason { get; } = reason;

	public ImmutableArray<string> MissingTypeNames { get; } = missingTypeNames;
}
