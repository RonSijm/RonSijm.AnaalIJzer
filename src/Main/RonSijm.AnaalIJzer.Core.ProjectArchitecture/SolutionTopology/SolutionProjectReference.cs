namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture.SolutionTopology;

public readonly struct SolutionProjectReference(
	string sourceProjectPath,
	string sourceProjectName,
	string targetProjectPath,
	string targetProjectName)
{
	public string SourceProjectPath { get; } = sourceProjectPath;

	public string SourceProjectName { get; } = sourceProjectName;

	public string TargetProjectPath { get; } = targetProjectPath;

	public string TargetProjectName { get; } = targetProjectName;
}
