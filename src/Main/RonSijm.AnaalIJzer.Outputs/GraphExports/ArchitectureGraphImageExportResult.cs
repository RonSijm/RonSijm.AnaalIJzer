using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportResult
{
	public ArchitectureGraphImageExportResult(
		int exitCode,
		int successCount,
		int placeholderCount,
		ImmutableArray<ArchitectureGraphImageExportFileResult> files)
	{
		ExitCode = exitCode;
		SuccessCount = successCount;
		PlaceholderCount = placeholderCount;
		Files = files.IsDefault ? ImmutableArray<ArchitectureGraphImageExportFileResult>.Empty : files;
	}

	public int ExitCode { get; }

	public int SuccessCount { get; }

	public int PlaceholderCount { get; }

	public ImmutableArray<ArchitectureGraphImageExportFileResult> Files { get; }
}
