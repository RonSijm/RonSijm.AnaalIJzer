namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct ProjectReferenceRule(
	ProjectReferenceRuleKind kind,
	string from,
	string to,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public ProjectReferenceRuleKind Kind { get; } = kind;

	public string From { get; } = from;

	public string To { get; } = to;

	public string? Description { get; } = description;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;
}
