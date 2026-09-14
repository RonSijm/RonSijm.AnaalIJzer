using System.Text.Json;
using System.Text.Json.Serialization;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

namespace RonSijm.Anaaltomy.CommandLine;

internal static class AnaaltomyOutput
{
	public static void WriteSummary(TextWriter output, StatisticsSummary summary)
	{
		output.WriteLine("Scan " + summary.Scan.ScanId + " " + summary.Scan.Status + ": " + summary.Scan.InputPath);
		output.WriteLine("Projects: " + summary.ProjectCount + "; failures: " + summary.FailureCount + ".");
		output.WriteLine();
		output.WriteLine("Dimension                 Bucket                         Count");
		foreach (var measurement in summary.Measurements
			         .OrderBy(measurement => measurement.Dimension)
			         .ThenBy(measurement => StatisticsDimensionCatalog.GetBucketOrder(measurement.Dimension, measurement.Bucket))
			         .ThenBy(measurement => measurement.Bucket, StringComparer.Ordinal))
		{
			output.WriteLine(measurement.Dimension.ToString().PadRight(25) + measurement.Bucket.PadRight(31) + measurement.Count);
		}
	}

	public static void WriteFailures(TextWriter output, IReadOnlyList<StatisticsScanFailure> failures)
	{
		if (failures.Count == 0)
		{
			return;
		}

		output.WriteLine();
		output.WriteLine("Failures:");
		foreach (var failure in failures
			         .OrderBy(failure => failure.Stage, StringComparer.Ordinal)
			         .ThenBy(failure => failure.ProjectPath, StringComparer.OrdinalIgnoreCase)
			         .ThenBy(failure => failure.Message, StringComparer.Ordinal))
		{
			var projectSuffix = string.IsNullOrWhiteSpace(failure.ProjectPath) ? string.Empty : " (" + failure.ProjectPath + ")";
			output.WriteLine("- [" + failure.Stage + "]" + projectSuffix + " " + failure.Message);
		}
	}

	public static void WriteTrend(TextWriter output, IReadOnlyList<StatisticsTrendPoint> trend)
	{
		output.WriteLine("Commit                                     Committed at                 Count");
		foreach (var point in trend)
		{
			output.WriteLine(point.CommitSha.PadRight(43) + (point.CommittedAtUtc?.ToString("u") ?? "unknown").PadRight(29) + point.Count);
		}
	}

	public static void WriteComparison(TextWriter output, IReadOnlyList<StatisticsComparison> comparison)
	{
		output.WriteLine("Dimension                 Bucket                         From       To      Delta");
		foreach (var item in comparison)
		{
			output.WriteLine(item.Dimension.ToString().PadRight(25) + item.Bucket.PadRight(31) + item.FromCount.ToString().PadLeft(8) + item.ToCount.ToString().PadLeft(9) + item.Delta.ToString().PadLeft(11));
		}
	}

	public static void WriteCommitChanges(TextWriter output, IReadOnlyList<StatisticsCommitChange> changes)
	{
		output.WriteLine("Commit                                     Committed at                 Previous    Count    Delta");
		foreach (var change in changes)
		{
			output.WriteLine(change.CommitSha.PadRight(43)
				+ change.CommittedAtUtc.ToString("u").PadRight(29)
				+ change.PreviousCount.ToString().PadLeft(9)
				+ change.Count.ToString().PadLeft(9)
				+ change.Delta.ToString().PadLeft(9));
		}
	}

	public static async Task WriteExportAsync(string format, string outputPath, StatisticsSummary summary, CancellationToken cancellationToken)
	{
		var fullOutputPath = Path.GetFullPath(outputPath);
		Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
		switch (format.ToLowerInvariant())
		{
			case "json":
				await using (var stream = File.Create(fullOutputPath))
				{
					await JsonSerializer.SerializeAsync(stream, summary, CreateJsonOptions(), cancellationToken);
				}
				break;
			case "csv":
				await using (var writer = new StreamWriter(fullOutputPath, false, new System.Text.UTF8Encoding(false)))
				{
					await writer.WriteLineAsync("Dimension,Bucket,Count");
					foreach (var measurement in summary.Measurements)
					{
						await writer.WriteLineAsync(measurement.Dimension + "," + EscapeCsv(measurement.Bucket) + "," + measurement.Count);
					}
				}
				break;
			default:
				throw new ArgumentException("Unsupported export format: " + format + ". Use json or csv.");
		}
	}

	private static JsonSerializerOptions CreateJsonOptions()
	{
		var result = new JsonSerializerOptions
		{
			WriteIndented = true,
			Converters = { new JsonStringEnumConverter() }
		};

		return result;
	}

	private static string EscapeCsv(string value)
	{
		var result = value.IndexOfAny([',', '"', '\r', '\n']) >= 0
			? "\"" + value.Replace("\"", "\"\"") + "\""
			: value;

		return result;
	}
}
