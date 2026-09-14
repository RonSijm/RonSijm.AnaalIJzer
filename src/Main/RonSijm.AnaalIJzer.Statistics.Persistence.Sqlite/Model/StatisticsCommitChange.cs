using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsCommitChange(string commitSha, DateTimeOffset committedAtUtc, StatisticsDimension dimension, string bucket, long previousCount, long count)
{
	public string CommitSha { get; } = commitSha;
	public DateTimeOffset CommittedAtUtc { get; } = committedAtUtc;
	public StatisticsDimension Dimension { get; } = dimension;
	public string Bucket { get; } = bucket;
	public long PreviousCount { get; } = previousCount;
	public long Count { get; } = count;
	public long Delta { get; } = count - previousCount;
}
