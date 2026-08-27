using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Core.Indicators;

public sealed class ArchitectureExposurePathSegment(string displayName, string? sourcePath, TextSpan? sourceSpan)
{
	public string DisplayName { get; } = displayName;
	public string? SourcePath { get; } = sourcePath;
	public TextSpan? SourceSpan { get; } = sourceSpan;
	public bool CanNavigate => !string.IsNullOrWhiteSpace(SourcePath) && SourceSpan is not null;
}
