namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Policies;

public readonly struct ForbiddenOperationPolicyEvaluation(
    ForbiddenOperationPolicy policy,
    ForbiddenOperationRule rule,
    string reason)
{
    public ForbiddenOperationPolicy Policy { get; } = policy;

    public ForbiddenOperationRule Rule { get; } = rule;

    public string Reason { get; } = reason;
}