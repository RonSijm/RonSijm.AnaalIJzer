namespace RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;

public readonly struct NamespaceHierarchyEvaluation(
	NamespaceHierarchyPolicy policy,
	NamespaceHierarchyRule rule,
	NamespaceHierarchyRelation relation,
	string reason)
{
	public NamespaceHierarchyPolicy Policy { get; } = policy;

	public NamespaceHierarchyRule Rule { get; } = rule;

	public NamespaceHierarchyRelation Relation { get; } = relation;

	public string Reason { get; } = reason;
}
