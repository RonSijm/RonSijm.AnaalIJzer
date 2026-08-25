namespace RonSijm.AnaalIJzer.Exceptions;

public readonly struct ArchitectureExceptionPolicy(
	bool isEnabled,
	bool requireReason,
	bool requireOwner,
	bool requireExpiresOn,
	int warnBeforeDays,
	string? description,
	string? sourcePath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public static ArchitectureExceptionPolicy Disabled { get; } = new(false, false, false, false, 14, null, null, 0, 0);

	public bool IsEnabled { get; } = isEnabled;

	public bool RequireReason { get; } = requireReason;

	public bool RequireOwner { get; } = requireOwner;

	public bool RequireExpiresOn { get; } = requireExpiresOn;

	public int WarnBeforeDays { get; } = warnBeforeDays;

	public string? Description { get; } = description;

	public string? SourcePath { get; } = sourcePath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public bool RequiresMetadata
	{
		get
		{
			var result = RequireReason || RequireOwner || RequireExpiresOn;

			return result;
		}
	}
}
