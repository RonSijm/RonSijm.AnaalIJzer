namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public readonly struct StatisticsGroupedMeasurement(StatisticsDimension dimension, string bucket, StatisticsDimension groupDimension, string groupBucket, long count)
{
	public StatisticsDimension Dimension { get; } = dimension;
	public string Bucket { get; } = bucket;
	public StatisticsDimension GroupDimension { get; } = groupDimension;
	public string GroupBucket { get; } = groupBucket;
	public long Count { get; } = count;
}
