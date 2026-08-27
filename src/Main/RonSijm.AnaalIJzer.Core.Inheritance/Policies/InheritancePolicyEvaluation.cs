namespace RonSijm.AnaalIJzer.Core.Inheritance.Policies;

public readonly struct InheritancePolicyEvaluation(
	InheritancePolicy policy,
	InheritanceViolationKind violationKind,
	string reason)
{
	public InheritancePolicy Policy { get; } = policy;

	public InheritanceViolationKind ViolationKind { get; } = violationKind;

	public string Reason { get; } = reason;
}
