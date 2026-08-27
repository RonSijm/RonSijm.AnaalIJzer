namespace RonSijm.AnaalIJzer.Core.Exceptions;

public readonly struct ArchitectureExceptionReview(
	string matcherKind,
	string matcherLabel,
	ArchitectureExceptionMetadata metadata,
	ArchitectureExceptionStatus status,
	string message,
	string ownerLayerPath,
	string xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string MatcherKind { get; } = matcherKind;

	public string MatcherLabel { get; } = matcherLabel;

	public ArchitectureExceptionMetadata Metadata { get; } = metadata;

	public ArchitectureExceptionStatus Status { get; } = status;

	public string Message { get; } = message;

	public string OwnerLayerPath { get; } = ownerLayerPath;

	public string XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;
}
