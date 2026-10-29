using RonSijm.IJzer.Statistics.GitHistory.Git;
using RonSijm.IJzer.Statistics.Persistence.Sqlite.Model;
using RonSijm.IJzer.Workspace.Loading;

namespace RonSijm.IJzer.Statistics.GitHistory.Scanning;

public sealed class StatisticsGitHistoryScanRequest(
	string repositoryPath,
	StatisticsScanDefinition definition,
	GitHistorySelection selection,
	bool resume = false,
	WorkspaceRestoreMode restoreMode = WorkspaceRestoreMode.Always)
{
	public string RepositoryPath { get; } = repositoryPath;
	public StatisticsScanDefinition Definition { get; } = definition;
	public GitHistorySelection Selection { get; } = selection;
	public bool Resume { get; } = resume;
	public WorkspaceRestoreMode RestoreMode { get; } = restoreMode;
}
