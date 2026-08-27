namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportFileResult(string inputPath, string outputPath, bool succeeded, bool placeholderWritten, string? message)
{
	public string InputPath { get; } = inputPath;

	public string OutputPath { get; } = outputPath;

	public bool Succeeded { get; } = succeeded;

	public bool PlaceholderWritten { get; } = placeholderWritten;

	public string? Message { get; } = message;
}
