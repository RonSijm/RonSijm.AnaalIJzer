namespace RonSijm.AnaalIJzer.Outputs.GraphExports;

public sealed class ArchitectureGraphImageExportRequest(ArchitectureGraphImageExportMode mode, string inputPath, string outputPath, bool failOnError)
{
	public ArchitectureGraphImageExportMode Mode { get; } = mode;

	public string InputPath { get; } = inputPath;

	public string OutputPath { get; } = outputPath;

	public bool FailOnError { get; } = failOnError;
}
