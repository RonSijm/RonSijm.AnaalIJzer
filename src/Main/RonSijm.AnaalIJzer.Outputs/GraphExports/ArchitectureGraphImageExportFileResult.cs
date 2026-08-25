namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportFileResult
{
	public ArchitectureGraphImageExportFileResult(string inputPath, string outputPath, bool succeeded, bool placeholderWritten, string? message)
	{
		InputPath = inputPath;
		OutputPath = outputPath;
		Succeeded = succeeded;
		PlaceholderWritten = placeholderWritten;
		Message = message;
	}

	public string InputPath { get; }

	public string OutputPath { get; }

	public bool Succeeded { get; }

	public bool PlaceholderWritten { get; }

	public string? Message { get; }
}
