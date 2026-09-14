namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public readonly struct StatisticsMeasurement(StatisticsDimension dimension, string bucket, long count)
{
	public StatisticsDimension Dimension { get; } = dimension;
	public string Bucket { get; } = bucket;
	public long Count { get; } = count;
}
