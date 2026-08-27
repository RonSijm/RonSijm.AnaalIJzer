namespace RonSijm.AnaalIJzer.Core.Contracts.Contracts;

public readonly struct ContractPolicyEvaluation(
	ContractPolicy policy,
	ContractViolationKind violationKind,
	string reason,
	ContractPropertyAccessor? propertyAccessor = null)
{
	public ContractPolicy Policy { get; } = policy;
	public ContractViolationKind ViolationKind { get; } = violationKind;
	public string Reason { get; } = reason;
	public ContractPropertyAccessor? PropertyAccessor { get; } = propertyAccessor;
}
