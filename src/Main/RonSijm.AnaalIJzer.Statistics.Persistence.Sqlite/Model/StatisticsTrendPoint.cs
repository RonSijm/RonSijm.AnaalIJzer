using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsTrendPoint(string commitSha, DateTimeOffset? committedAtUtc, StatisticsDimension dimension, string bucket, long count)
{
	public string CommitSha { get; } = commitSha;
	public DateTimeOffset? CommittedAtUtc { get; } = committedAtUtc;
	public StatisticsDimension Dimension { get; } = dimension;
	public string Bucket { get; } = bucket;
	public long Count { get; } = count;
}
