namespace RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

public readonly struct ReturnValuePolicyEvaluation(
    ReturnValuePolicy policy,
    ReturnValueRule rule,
    ReturnValuePolicyRuleMode ruleMode,
    string reason)
{
    public ReturnValuePolicy Policy { get; } = policy;

    public ReturnValueRule Rule { get; } = rule;

    public ReturnValuePolicyRuleMode RuleMode { get; } = ruleMode;

    public string Reason { get; } = reason;
}