using RonSijm.AnaalIJzer.Core.Statistics.Model;

namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Scanning;

public sealed class StatisticsGitHistoryScanResult(int selectedCommitCount, int scannedCommitCount, int skippedCompleteCommitCount, IReadOnlyList<StatisticsScanFailure> failures)
{
	public int SelectedCommitCount { get; } = selectedCommitCount;
	public int ScannedCommitCount { get; } = scannedCommitCount;
	public int SkippedCompleteCommitCount { get; } = skippedCompleteCommitCount;
	public IReadOnlyList<StatisticsScanFailure> Failures { get; } = failures;
	public bool IsComplete => Failures.Count == 0;
}
