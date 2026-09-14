namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public readonly struct StatisticsProjectIdentity(string projectPath, string projectName, string? assemblyName, string? targetFramework)
{
	public string ProjectPath { get; } = projectPath;
	public string ProjectName { get; } = projectName;
	public string? AssemblyName { get; } = assemblyName;
	public string? TargetFramework { get; } = targetFramework;
}
