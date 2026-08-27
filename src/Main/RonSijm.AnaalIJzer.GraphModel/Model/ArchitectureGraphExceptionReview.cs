namespace RonSijm.AnaalIJzer.GraphModel.Model;

public sealed class ArchitectureGraphExceptionReview(
	string ownerLayerPath,
	string matcherKind,
	string matcherLabel,
	string status,
	string message,
	string? reason,
	string? owner,
	string? expiresOn,
	string sourcePath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public string OwnerLayerPath { get; } = ownerLayerPath;

	public string MatcherKind { get; } = matcherKind;

	public string MatcherLabel { get; } = matcherLabel;

	public string Status { get; } = status;

	public string Message { get; } = message;

	public string? Reason { get; } = reason;

	public string? Owner { get; } = owner;

	public string? ExpiresOn { get; } = expiresOn;

	public string SourcePath { get; } = sourcePath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;
}
