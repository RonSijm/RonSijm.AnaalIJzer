using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsPersistRequest(
	StatisticsScanDefinition definition,
	StatisticsScanSnapshot snapshot,
	StatisticsRepositoryIdentity? repository = null,
	StatisticsGitCommit? commit = null)
{
	public StatisticsScanDefinition Definition { get; } = definition;
	public StatisticsScanSnapshot Snapshot { get; } = snapshot;
	public StatisticsRepositoryIdentity? Repository { get; } = repository;
	public StatisticsGitCommit? Commit { get; } = commit;
}
