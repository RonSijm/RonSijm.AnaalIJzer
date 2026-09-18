using RonSijm.AnaalIJzer.Core.DependencyRules;

namespace RonSijm.AnaalIJzer.Core.NamespaceHierarchy.Policies;

public readonly struct NamespaceHierarchyRule(
	NamespaceHierarchyRelation relation,
	DependencySiteFilter siteFilter,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public NamespaceHierarchyRelation Relation { get; } = relation;

	public DependencySiteFilter SiteFilter { get; } = siteFilter;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool AppliesToSite(string site)
	{
		var result = SiteFilter.Allows(site);

		return result;
	}
}
