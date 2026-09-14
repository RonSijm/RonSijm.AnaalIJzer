using System.Globalization;
using System.Text;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.Anaaltomy.Charting.Model;

namespace RonSijm.Anaaltomy.Charting.Rendering;

public sealed class StatisticsChartReportGenerator
{
	private const int ChartWidth = 1600;
	private const int MinimumChartHeight = 360;
	private const int RowHeight = 48;
	private const int ChartChromeHeight = 150;
	private const int TrendChartHeight = 640;
	private static readonly ScottPlot.Color[] SeriesColors = ScottPlot.Color.FromHex(
	[
		"#2563eb",
		"#dc2626",
		"#16a34a",
		"#ca8a04",
		"#9333ea",
		"#0891b2",
		"#db2777",
		"#ea580c",
		"#4f46e5",
		"#65a30d"
	]);

	public StatisticsChartReport Generate(string outputDirectory, IReadOnlyList<StatisticsMeasurement> measurements, StatisticsDimension? requestedDimension = null, string? subject = null)
	{
		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
		}

		ArgumentNullException.ThrowIfNull(measurements);

		var fullOutputDirectory = Path.GetFullPath(outputDirectory);
		Directory.CreateDirectory(fullOutputDirectory);
		var outputPaths = new List<string>();
		var dimensions = requestedDimension is { } dimension
			? [dimension]
			: Enum.GetValues<StatisticsDimension>();

		foreach (var currentDimension in dimensions)
		{
			var dimensionMeasurements = measurements
				.Where(measurement => measurement.Dimension == currentDimension && measurement.Count > 0)
				.OrderByDescending(measurement => measurement.Count)
				.ThenBy(measurement => StatisticsDimensionCatalog.GetBucketOrder(currentDimension, measurement.Bucket))
				.ThenBy(measurement => measurement.Bucket, StringComparer.Ordinal)
				.ToArray();
			if (dimensionMeasurements.Length == 0)
			{
				continue;
			}

			var outputPath = Path.Combine(fullOutputDirectory, GetFileName(currentDimension));
			RenderDimension(outputPath, currentDimension, dimensionMeasurements, subject);
			outputPaths.Add(outputPath);
		}

		if (outputPaths.Count == 0)
		{
			var dimensionSuffix = requestedDimension is { } requestedDimensionValue ? " for '" + requestedDimensionValue + "'" : string.Empty;
			throw new ArgumentException("The statistics contain no chartable measurements" + dimensionSuffix + ".", nameof(measurements));
		}

		var result = new StatisticsChartReport(outputPaths);

		return result;
	}

	public StatisticsChartReport GenerateTrend(string outputDirectory, StatisticsDimension dimension, string bucket, IReadOnlyList<StatisticsTrendChartPoint> points)
	{
		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
		}

		if (string.IsNullOrWhiteSpace(bucket))
		{
			throw new ArgumentException("A statistics bucket is required.", nameof(bucket));
		}

		ArgumentNullException.ThrowIfNull(points);
		if (points.Count == 0)
		{
			throw new ArgumentException("At least one history point is required to render a trend chart.", nameof(points));
		}

		var fullOutputDirectory = Path.GetFullPath(outputDirectory);
		Directory.CreateDirectory(fullOutputDirectory);
		var outputPath = Path.Combine(fullOutputDirectory, GetTrendFileName(dimension, bucket));
		RenderTrend(outputPath, dimension, bucket, points);
		var result = new StatisticsChartReport([outputPath]);

		return result;
	}

	public StatisticsChartReport GenerateGrouped(string outputDirectory, IReadOnlyList<StatisticsGroupedMeasurement> measurements, StatisticsDimension? requestedDimension = null, StatisticsDimension? requestedGroupDimension = null, string? subject = null)
	{
		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
		}

		ArgumentNullException.ThrowIfNull(measurements);

		var fullOutputDirectory = Path.GetFullPath(outputDirectory);
		Directory.CreateDirectory(fullOutputDirectory);
		var outputPaths = new List<string>();
		var groups = measurements
			.Where(measurement => measurement.Count > 0)
			.Where(measurement => requestedDimension is null || measurement.Dimension == requestedDimension)
			.Where(measurement => requestedGroupDimension is null || measurement.GroupDimension == requestedGroupDimension)
			.GroupBy(measurement => (measurement.Dimension, measurement.GroupDimension))
			.OrderBy(group => group.Key.Dimension)
			.ThenBy(group => group.Key.GroupDimension);

		foreach (var group in groups)
		{
			var dimensionMeasurements = group.ToArray();
			if (dimensionMeasurements.Length == 0)
			{
				continue;
			}

			var outputPath = Path.Combine(fullOutputDirectory, GetGroupedFileName(group.Key.Dimension, group.Key.GroupDimension));
			RenderGroupedDimension(outputPath, group.Key.Dimension, group.Key.GroupDimension, dimensionMeasurements, subject);
			outputPaths.Add(outputPath);
		}

		if (outputPaths.Count == 0)
		{
			var dimensionSuffix = requestedDimension is { } requestedDimensionValue ? " for '" + requestedDimensionValue + "'" : string.Empty;
			var groupSuffix = requestedGroupDimension is { } requestedGroupDimensionValue ? " grouped by '" + requestedGroupDimensionValue + "'" : string.Empty;
			throw new ArgumentException("The statistics contain no chartable grouped measurements" + dimensionSuffix + groupSuffix + ".", nameof(measurements));
		}

		var result = new StatisticsChartReport(outputPaths);

		return result;
	}

	private static void RenderDimension(string outputPath, StatisticsDimension dimension, IReadOnlyList<StatisticsMeasurement> measurements, string? subject)
	{
		var values = measurements.Select(measurement => Convert.ToDouble(measurement.Count, CultureInfo.InvariantCulture)).ToArray();
		var positions = Enumerable.Range(0, measurements.Count).Select(index => (double)index).ToArray();
		var labels = measurements
			.Select(measurement => measurement.Bucket + " (" + measurement.Count.ToString("N0", CultureInfo.InvariantCulture) + ")")
			.ToArray();
		var height = Math.Max(MinimumChartHeight, ChartChromeHeight + measurements.Count * RowHeight);
		var plot = new ScottPlot.Plot();
		var bars = plot.Add.Bars(values);
		bars.Horizontal = true;
		bars.Color = SeriesColors[0];
		plot.Axes.Left.SetTicks(positions, labels);
		plot.Axes.Left.MinimumSize = GetLabelAxisWidth(labels);
		plot.Title(GetBreakdownTitle(dimension, subject));
		plot.XLabel("Occurrences");
		plot.SavePng(outputPath, ChartWidth, height);
	}

	private static void RenderTrend(string outputPath, StatisticsDimension dimension, string bucket, IReadOnlyList<StatisticsTrendChartPoint> points)
	{
		var positions = Enumerable.Range(0, points.Count).Select(index => (double)index).ToArray();
		var values = points.Select(point => Convert.ToDouble(point.Count, CultureInfo.InvariantCulture)).ToArray();
		var ticks = CreateTrendTicks(points);
		var plot = new ScottPlot.Plot();
		var line = plot.Add.Scatter(positions, values);
		line.LineWidth = 2;
		line.MarkerSize = 7;
		line.Color = SeriesColors[0];
		plot.Axes.Bottom.SetTicks(ticks.Positions, ticks.Labels);
		plot.Axes.Bottom.MinimumSize = 90;
		plot.Title("Anaaltomy " + GetDisplayName(dimension) + " '" + bucket + "' history");
		plot.XLabel("Commits in stored history");
		plot.YLabel("Occurrences");
		plot.SavePng(outputPath, ChartWidth, TrendChartHeight);
	}

	private static void RenderGroupedDimension(string outputPath, StatisticsDimension dimension, StatisticsDimension groupDimension, IReadOnlyList<StatisticsGroupedMeasurement> measurements, string? subject)
	{
		var bucketTotals = measurements
			.GroupBy(measurement => measurement.Bucket, StringComparer.Ordinal)
			.Select(group => new { Bucket = group.Key, Count = group.Sum(measurement => measurement.Count) })
			.OrderByDescending(item => item.Count)
			.ThenBy(item => StatisticsDimensionCatalog.GetBucketOrder(dimension, item.Bucket))
			.ThenBy(item => item.Bucket, StringComparer.Ordinal)
			.ToArray();
		var buckets = bucketTotals.Select(item => item.Bucket).ToArray();
		var groupBuckets = measurements
			.Select(measurement => measurement.GroupBucket)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(bucket => StatisticsDimensionCatalog.GetBucketOrder(groupDimension, bucket))
			.ThenBy(bucket => bucket, StringComparer.Ordinal)
			.ToArray();
		var lookup = measurements
			.GroupBy(measurement => (measurement.Bucket, measurement.GroupBucket))
			.ToDictionary(
				group => group.Key,
				group => group.Sum(measurement => measurement.Count));
		var plot = new ScottPlot.Plot();
		var barSize = Math.Min(0.72, 0.82 / Math.Max(1, groupBuckets.Length));
		for (var groupIndex = 0; groupIndex < groupBuckets.Length; groupIndex++)
		{
			var groupBucket = groupBuckets[groupIndex];
			var color = GetSeriesColor(groupIndex);
			var offset = (groupIndex - (groupBuckets.Length - 1) / 2d) * barSize;
			var bars = new ScottPlot.Bar[buckets.Length];
			for (var bucketIndex = 0; bucketIndex < buckets.Length; bucketIndex++)
			{
				lookup.TryGetValue((buckets[bucketIndex], groupBucket), out var count);
				bars[bucketIndex] = new ScottPlot.Bar
				{
					Position = bucketIndex + offset,
					Value = Convert.ToDouble(count, CultureInfo.InvariantCulture),
					Size = barSize * 0.9,
					FillColor = color,
					LineColor = color.Darken(0.25)
				};
			}

			var barPlot = plot.Add.Bars(bars);
			barPlot.Horizontal = true;
			barPlot.Color = color;
			barPlot.LegendText = groupBucket;
		}

		var positions = Enumerable.Range(0, buckets.Length).Select(index => (double)index).ToArray();
		var labels = bucketTotals
			.Select(item => item.Bucket + " (" + item.Count.ToString("N0", CultureInfo.InvariantCulture) + ")")
			.ToArray();
		var height = Math.Max(MinimumChartHeight, ChartChromeHeight + buckets.Length * Math.Max(RowHeight, groupBuckets.Length * 18));
		plot.Axes.Left.SetTicks(positions, labels);
		plot.Axes.Left.MinimumSize = GetLabelAxisWidth(labels);
		plot.Title(GetGroupedBreakdownTitle(dimension, groupDimension, subject));
		plot.XLabel("Occurrences");
		plot.ShowLegend(ScottPlot.Edge.Right);
		plot.SavePng(outputPath, ChartWidth, height);
	}

	private static (double[] Positions, string[] Labels) CreateTrendTicks(IReadOnlyList<StatisticsTrendChartPoint> points)
	{
		var maximumTickCount = Math.Min(points.Count, 8);
		var indices = Enumerable.Range(0, maximumTickCount)
			.Select(index => maximumTickCount == 1
				? 0
				: (int)Math.Round(index * (points.Count - 1d) / (maximumTickCount - 1d), MidpointRounding.AwayFromZero))
			.Distinct()
			.ToArray();
		var positions = indices.Select(index => (double)index).ToArray();
		var labels = indices.Select(index => GetTrendPointLabel(points[index])).ToArray();
		var result = (positions, labels);

		return result;
	}

	private static string GetTrendPointLabel(StatisticsTrendChartPoint point)
	{
		var shortSha = point.CommitSha.Length <= 8 ? point.CommitSha : point.CommitSha[..8];
		var result = point.CommittedAtUtc is { } committedAtUtc
			? shortSha + "\n" + committedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
			: shortSha;

		return result;
	}

	private static ScottPlot.Color GetSeriesColor(int index)
	{
		var result = SeriesColors[index % SeriesColors.Length];

		return result;
	}

	private static int GetLabelAxisWidth(IReadOnlyList<string> labels)
	{
		var longestLabelLength = labels.Max(label => label.Length);
		var result = Math.Clamp(130 + longestLabelLength * 7, 220, 620);

		return result;
	}

	private static string GetDisplayName(StatisticsDimension dimension)
	{
		var result = dimension switch
		{
			StatisticsDimension.TypeKind => "Type Kind",
			StatisticsDimension.DependencySite => "Dependency Site",
			StatisticsDimension.TypeAccessibility => "Type Accessibility",
			StatisticsDimension.MemberAccessibility => "Member Accessibility",
			StatisticsDimension.MemberKind => "Member Kind",
			_ => dimension.ToString()
		};

		return result;
	}

	private static string GetBreakdownTitle(StatisticsDimension dimension, string? subject)
	{
		var result = "Anaaltomy " + GetDisplayName(dimension) + " breakdown" + GetSubjectSuffix(subject);

		return result;
	}

	private static string GetGroupedBreakdownTitle(StatisticsDimension dimension, StatisticsDimension groupDimension, string? subject)
	{
		var result = "Anaaltomy " + GetDisplayName(dimension) + " by " + GetDisplayName(groupDimension) + " breakdown" + GetSubjectSuffix(subject);

		return result;
	}

	private static string GetSubjectSuffix(string? subject)
	{
		if (string.IsNullOrWhiteSpace(subject))
		{
			return string.Empty;
		}

		var result = " of '" + subject.Trim() + "'";

		return result;
	}

	private static string GetFileName(StatisticsDimension dimension)
	{
		var result = dimension switch
		{
			StatisticsDimension.TypeKind => "type-kinds.png",
			StatisticsDimension.DependencySite => "dependency-sites.png",
			StatisticsDimension.TypeAccessibility => "type-accessibility.png",
			StatisticsDimension.MemberAccessibility => "member-accessibility.png",
			StatisticsDimension.MemberKind => "member-kinds.png",
			_ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown statistics dimension.")
		};

		return result;
	}

	private static string GetTrendFileName(StatisticsDimension dimension, string bucket)
	{
		var fileName = GetFileName(dimension);
		var stem = Path.GetFileNameWithoutExtension(fileName);
		var result = stem + "-" + ToFileNameSegment(bucket) + "-trend.png";

		return result;
	}

	private static string GetGroupedFileName(StatisticsDimension dimension, StatisticsDimension groupDimension)
	{
		var result = Path.GetFileNameWithoutExtension(GetFileName(dimension)) + "-by-" + Path.GetFileNameWithoutExtension(GetFileName(groupDimension)) + ".png";

		return result;
	}

	private static string ToFileNameSegment(string value)
	{
		var builder = new StringBuilder(value.Length);
		var previousWasSeparator = false;
		foreach (var character in value)
		{
			if (char.IsLetterOrDigit(character))
			{
				builder.Append(char.ToLowerInvariant(character));
				previousWasSeparator = false;
			}
			else if (!previousWasSeparator && builder.Length > 0)
			{
				builder.Append('-');
				previousWasSeparator = true;
			}
		}

		var result = builder.ToString().Trim('-');
		if (result.Length == 0)
		{
			result = "bucket";
		}

		return result;
	}
}
