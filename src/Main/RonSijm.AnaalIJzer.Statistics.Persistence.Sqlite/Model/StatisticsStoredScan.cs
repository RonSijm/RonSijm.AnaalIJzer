using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsStoredScan(long scanId, long definitionId, long? repositoryId, string? commitSha, string inputPath, StatisticsScanStatus status)
{
	public long ScanId { get; } = scanId;
	public long DefinitionId { get; } = definitionId;
	public long? RepositoryId { get; } = repositoryId;
	public string? CommitSha { get; } = commitSha;
	public string InputPath { get; } = inputPath;
	public StatisticsScanStatus Status { get; } = status;
}
