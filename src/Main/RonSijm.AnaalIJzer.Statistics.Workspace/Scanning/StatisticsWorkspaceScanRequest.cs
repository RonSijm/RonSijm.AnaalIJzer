using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;

public sealed class StatisticsWorkspaceScanRequest(
	StatisticsWorkspaceInputKind inputKind,
	string inputPath,
	string configuration = "Release",
	string? targetFramework = null,
	bool includeGeneratedCode = false,
	WorkspaceRestoreMode restoreMode = WorkspaceRestoreMode.Auto)
{
	public StatisticsWorkspaceInputKind InputKind { get; } = inputKind;
	public string InputPath { get; } = inputPath;
	public string Configuration { get; } = configuration;
	public string? TargetFramework { get; } = targetFramework;
	public bool IncludeGeneratedCode { get; } = includeGeneratedCode;
	public WorkspaceRestoreMode RestoreMode { get; } = restoreMode;
}
