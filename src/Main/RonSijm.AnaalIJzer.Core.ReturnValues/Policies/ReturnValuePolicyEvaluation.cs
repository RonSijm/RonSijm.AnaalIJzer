namespace RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

public readonly struct ReturnValuePolicyEvaluation(
	ReturnValuePolicy policy,
	ReturnValueRule rule,
	string reason)
{
	public ReturnValuePolicy Policy { get; } = policy;

	public ReturnValueRule Rule { get; } = rule;

	public string Reason { get; } = reason;
}
