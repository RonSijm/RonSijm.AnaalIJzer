using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportResult(
	int exitCode,
	int successCount,
	int placeholderCount,
	ImmutableArray<ArchitectureGraphImageExportFileResult> files)
{
	public int ExitCode { get; } = exitCode;

	public int SuccessCount { get; } = successCount;

	public int PlaceholderCount { get; } = placeholderCount;

	public ImmutableArray<ArchitectureGraphImageExportFileResult> Files { get; } = files.IsDefault ? ImmutableArray<ArchitectureGraphImageExportFileResult>.Empty : files;
}
