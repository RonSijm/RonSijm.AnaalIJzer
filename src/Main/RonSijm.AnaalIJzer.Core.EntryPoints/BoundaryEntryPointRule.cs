using RonSijm.AnaalIJzer.Core.DependencyRules;

namespace RonSijm.AnaalIJzer.Core.EntryPoints;

public readonly struct BoundaryEntryPointRule(
	BoundaryEntryPointSelector selector,
	DependencySiteFilter siteFilter,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public BoundaryEntryPointSelector Selector { get; } = selector;

	public DependencySiteFilter SiteFilter { get; } = siteFilter;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public string ToDisplayText()
	{
		var result = Selector.ToDisplayText();

		return result;
	}
}
