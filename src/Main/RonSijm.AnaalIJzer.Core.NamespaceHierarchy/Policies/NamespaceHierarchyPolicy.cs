using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;

public readonly struct NamespaceHierarchyPolicy(
	NamespaceHierarchyPath rootPath,
	ImmutableArray<NamespaceHierarchyRule> rules,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	private readonly ImmutableArray<NamespaceHierarchyRule> _rules = rules.IsDefault ? ImmutableArray<NamespaceHierarchyRule>.Empty : rules;

	public NamespaceHierarchyPath RootPath { get; } = rootPath;

	public string RootNamespace => RootPath.ToString();

	public ImmutableArray<NamespaceHierarchyRule> Rules => _rules;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public NamespaceHierarchyEvaluation? Evaluate(string callerNamespace, string dependencyNamespace, string site)
	{
		if (!NamespaceHierarchyPath.TryParse(callerNamespace, out var callerPath)
			|| !NamespaceHierarchyPath.TryParse(dependencyNamespace, out var dependencyPath)
			|| !RootPath.IsAncestorOfOrEqualTo(callerPath)
			|| !RootPath.IsAncestorOfOrEqualTo(dependencyPath))
		{
			return null;
		}

		var relation = callerPath.GetRelationTo(dependencyPath);
		foreach (var rule in Rules)
		{
			if (rule.Relation != relation || !rule.AppliesToSite(site))
			{
				continue;
			}

			var result = new NamespaceHierarchyEvaluation(
				this,
				rule,
				relation,
				$"NamespaceHierarchyPolicy '{RootNamespace}' blocks {relation} dependencies.");

			return result;
		}

		return null;
	}
}
