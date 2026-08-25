namespace RonSijm.AnaalIJzer.Engine.ApiSurface;

public readonly struct TransitiveExposureOptions(
	int maxDepth,
	string? description,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public const int DefaultMaxDepth = 3;
	public const int MaximumMaxDepth = 10;

	public int MaxDepth { get; } = maxDepth;
	public string? Description { get; } = description;
	public string XmlPath { get; } = xmlPath;
	public int XmlLineNumber { get; } = xmlLineNumber;
	public int XmlLinePosition { get; } = xmlLinePosition;
}
