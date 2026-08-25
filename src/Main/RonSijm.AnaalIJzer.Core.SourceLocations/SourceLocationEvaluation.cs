namespace RonSijm.AnaalIJzer.SourceLocations;

public readonly struct SourceLocationEvaluation(
	SourceLocationPolicy policy,
	string sourceFilePath,
	string normalizedSourcePath,
	string sourceAssemblyName,
	string reason)
{
	public SourceLocationPolicy Policy { get; } = policy;

	public string SourceFilePath { get; } = sourceFilePath;

	public string NormalizedSourcePath { get; } = normalizedSourcePath;

	public string SourceAssemblyName { get; } = sourceAssemblyName;

	public string Reason { get; } = reason;
}
