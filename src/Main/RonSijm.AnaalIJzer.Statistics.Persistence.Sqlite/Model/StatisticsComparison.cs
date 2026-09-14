using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsComparison(StatisticsDimension dimension, string bucket, long fromCount, long toCount)
{
	public StatisticsDimension Dimension { get; } = dimension;
	public string Bucket { get; } = bucket;
	public long FromCount { get; } = fromCount;
	public long ToCount { get; } = toCount;
	public long Delta { get; } = toCount - fromCount;
}
