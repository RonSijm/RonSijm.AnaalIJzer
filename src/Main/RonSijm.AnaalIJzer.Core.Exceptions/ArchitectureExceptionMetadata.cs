namespace RonSijm.AnaalIJzer.Core.Exceptions;

public readonly struct ArchitectureExceptionMetadata(string? reason, string? owner, string? expiresOnText, DateTime? expiresOn)
{
	public string? Reason { get; } = reason;

	public string? Owner { get; } = owner;

	public string? ExpiresOnText { get; } = expiresOnText;

	public DateTime? ExpiresOn { get; } = expiresOn;
}
