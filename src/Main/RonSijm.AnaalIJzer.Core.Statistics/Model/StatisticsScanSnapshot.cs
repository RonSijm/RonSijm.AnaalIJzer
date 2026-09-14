namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public sealed class StatisticsScanSnapshot(string inputPath, StatisticsScanStatus status, IReadOnlyList<StatisticsProjectSnapshot> projects, IReadOnlyList<StatisticsScanFailure> failures)
{
	public string InputPath { get; } = inputPath;
	public StatisticsScanStatus Status { get; } = status;
	public IReadOnlyList<StatisticsProjectSnapshot> Projects { get; } = projects;
	public IReadOnlyList<StatisticsScanFailure> Failures { get; } = failures;
}
