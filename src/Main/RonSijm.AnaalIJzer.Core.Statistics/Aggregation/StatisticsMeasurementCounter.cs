using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Core.Statistics.Aggregation;

public sealed class StatisticsMeasurementCounter
{
	private readonly Dictionary<(StatisticsDimension Dimension, string Bucket), long> _counts = [];
	private readonly Dictionary<(StatisticsDimension Dimension, string Bucket, StatisticsDimension GroupDimension, string GroupBucket), long> _groupedCounts = [];

	public void Record(StatisticsDimension dimension, string bucket)
	{
		Record(dimension, bucket, 1);
	}

	public void Record(StatisticsDimension dimension, string bucket, long count)
	{
		if (string.IsNullOrWhiteSpace(bucket))
		{
			throw new ArgumentException("A statistics bucket is required.", nameof(bucket));
		}

		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count), "A statistics count must be greater than zero.");
		}

		var key = (dimension, bucket);
		_counts.TryGetValue(key, out var currentCount);
		_counts[key] = currentCount + count;
	}

	public void RecordGrouped(StatisticsDimension dimension, string bucket, StatisticsDimension groupDimension, string groupBucket)
	{
		RecordGrouped(dimension, bucket, groupDimension, groupBucket, 1);
	}

	public void RecordGrouped(StatisticsDimension dimension, string bucket, StatisticsDimension groupDimension, string groupBucket, long count)
	{
		if (string.IsNullOrWhiteSpace(bucket))
		{
			throw new ArgumentException("A statistics bucket is required.", nameof(bucket));
		}

		if (string.IsNullOrWhiteSpace(groupBucket))
		{
			throw new ArgumentException("A statistics group bucket is required.", nameof(groupBucket));
		}

		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count), "A statistics count must be greater than zero.");
		}

		var key = (dimension, bucket, groupDimension, groupBucket);
		_groupedCounts.TryGetValue(key, out var currentCount);
		_groupedCounts[key] = currentCount + count;
	}

	public IReadOnlyList<StatisticsMeasurement> GetMeasurements()
	{
		var result = _counts
			.Select(item => new StatisticsMeasurement(item.Key.Dimension, item.Key.Bucket, item.Value))
			.OrderBy(item => item.Dimension)
			.ThenBy(item => StatisticsDimensionCatalog.GetBucketOrder(item.Dimension, item.Bucket))
			.ThenBy(item => item.Bucket, StringComparer.Ordinal)
			.ToArray();

		return result;
	}

	public IReadOnlyList<StatisticsGroupedMeasurement> GetGroupedMeasurements()
	{
		var result = _groupedCounts
			.Select(item => new StatisticsGroupedMeasurement(item.Key.Dimension, item.Key.Bucket, item.Key.GroupDimension, item.Key.GroupBucket, item.Value))
			.OrderBy(item => item.Dimension)
			.ThenBy(item => item.GroupDimension)
			.ThenBy(item => StatisticsDimensionCatalog.GetBucketOrder(item.Dimension, item.Bucket))
			.ThenBy(item => item.Bucket, StringComparer.Ordinal)
			.ThenBy(item => StatisticsDimensionCatalog.GetBucketOrder(item.GroupDimension, item.GroupBucket))
			.ThenBy(item => item.GroupBucket, StringComparer.Ordinal)
			.ToArray();

		return result;
	}
}
