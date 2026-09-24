namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;

/// <summary>The first deterministic reason an assembly attribute violates a configured policy.</summary>
public readonly struct AssemblyAttributePolicyEvaluation(AssemblyAttributePolicy policy, AssemblyAttributeRule rule, string reason)
{
    public AssemblyAttributePolicy Policy { get; } = policy;

    public AssemblyAttributeRule Rule { get; } = rule;

    public string Reason { get; } = reason;
}