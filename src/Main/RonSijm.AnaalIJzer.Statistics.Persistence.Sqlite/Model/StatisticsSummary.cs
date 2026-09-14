using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsSummary(
	StatisticsStoredScan scan,
	IReadOnlyList<StatisticsMeasurement> measurements,
	int projectCount,
	int failureCount,
	IReadOnlyList<StatisticsGroupedMeasurement>? groupedMeasurements = null)
{
	public StatisticsStoredScan Scan { get; } = scan;
	public IReadOnlyList<StatisticsMeasurement> Measurements { get; } = measurements;
	public IReadOnlyList<StatisticsGroupedMeasurement> GroupedMeasurements { get; } = groupedMeasurements ?? [];
	public int ProjectCount { get; } = projectCount;
	public int FailureCount { get; } = failureCount;
}
