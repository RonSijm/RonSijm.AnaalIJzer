namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;

public readonly struct BehavioralOperationPolicyEvaluation(
	BehavioralOperationPolicy policy,
	BehavioralOperationRule rule,
	BehavioralOperationViolationKind violationKind,
	string reason,
	SemanticOperationOccurrence? occurrence)
{
	public BehavioralOperationPolicy Policy { get; } = policy;

	public BehavioralOperationRule Rule { get; } = rule;

	public BehavioralOperationViolationKind ViolationKind { get; } = violationKind;

	public string Reason { get; } = reason;

	public SemanticOperationOccurrence? Occurrence { get; } = occurrence;
}
