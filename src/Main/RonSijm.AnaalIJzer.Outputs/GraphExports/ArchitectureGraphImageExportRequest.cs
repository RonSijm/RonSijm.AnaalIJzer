namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportRequest
{
	public ArchitectureGraphImageExportRequest(ArchitectureGraphImageExportMode mode, string inputPath, string outputPath, bool failOnError)
	{
		Mode = mode;
		InputPath = inputPath;
		OutputPath = outputPath;
		FailOnError = failOnError;
	}

	public ArchitectureGraphImageExportMode Mode { get; }

	public string InputPath { get; }

	public string OutputPath { get; }

	public bool FailOnError { get; }
}
