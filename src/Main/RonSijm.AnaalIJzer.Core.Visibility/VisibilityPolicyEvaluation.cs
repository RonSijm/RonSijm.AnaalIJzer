namespace RonSijm.AnaalIJzer.Core.Visibility;

public readonly struct VisibilityPolicyEvaluation(
	VisibilityPolicy policy,
	VisibilityPolicyTarget target,
	ArchitectureAccessibility accessibility,
	string reason)
{
	public VisibilityPolicy Policy { get; } = policy;
	public VisibilityPolicyTarget Target { get; } = target;
	public ArchitectureAccessibility Accessibility { get; } = accessibility;
	public string Reason { get; } = reason;
}
