namespace RonSijm.AnaalIJzer.Core.Contracts.Contracts;

public readonly struct ContractPolicyEvaluation(
	ContractPolicy policy,
	ContractViolationKind violationKind,
	string reason)
{
	public ContractPolicy Policy { get; } = policy;
	public ContractViolationKind ViolationKind { get; } = violationKind;
	public string Reason { get; } = reason;
}
