using RonSijm.IJzer.Core.Statistics.Model;
using RonSijm.IJzer.Statistics.GitHistory.Git;
using RonSijm.IJzer.Statistics.GitHistory.Scanning;
using RonSijm.IJzer.Statistics.GitHistory.Tests.Git;
using RonSijm.IJzer.Statistics.Persistence.Sqlite.Model;
using RonSijm.IJzer.Statistics.Persistence.Sqlite.Storage;
using RonSijm.IJzer.Statistics.Workspace.Scanning;

namespace RonSijm.IJzer.Statistics.GitHistory.Tests.Scanning;

public sealed class StatisticsGitHistoryScannerTests
{
	[Fact]
	public async Task ScanAsync_PersistsCommitHistoryAndResumeSkipsCompletedCommits()
	{
		await using var repository = await GitTestRepository.CreateWithMergeAsync(TestContext.Current.CancellationToken);
		var databaseDirectory = Path.Combine(Path.GetTempPath(), "IJzer.Gauge", "GitHistoryTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(databaseDirectory);
		try
		{
			var runner = new GitCommandRunner();
			var database = new StatisticsSqliteDatabase(Path.Combine(databaseDirectory, "statistics.db"));
			var scanner = new StatisticsGitHistoryScanner(
				new GitRepositoryReader(runner),
				runner,
				new StatisticsWorkspaceScanner(),
				database);
			var definition = new StatisticsScanDefinition("Release", "net10.0", false, "1");
			var request = new StatisticsGitHistoryScanRequest(repository.RootPath, definition, new GitHistorySelection(true));
			var activeHeadBefore = await runner.RunRequiredAsync(repository.RootPath, ["rev-parse", "HEAD"], TestContext.Current.CancellationToken);

			var first = await scanner.ScanAsync(request, null, TestContext.Current.CancellationToken);
			var resumed = await scanner.ScanAsync(new StatisticsGitHistoryScanRequest(repository.RootPath, definition, new GitHistorySelection(true), resume: true), null, TestContext.Current.CancellationToken);
			var activeHeadAfter = await runner.RunRequiredAsync(repository.RootPath, ["rev-parse", "HEAD"], TestContext.Current.CancellationToken);
			var trend = await database.ReadTrendAsync(StatisticsDimension.TypeKind, "Class", TestContext.Current.CancellationToken);

			first.IsComplete.Should().BeTrue();
			first.SelectedCommitCount.Should().BeGreaterThanOrEqualTo(4);
			first.ScannedCommitCount.Should().Be(first.SelectedCommitCount);
			resumed.ScannedCommitCount.Should().Be(0);
			resumed.SkippedCompleteCommitCount.Should().Be(first.SelectedCommitCount);
			trend.Should().HaveCount(first.SelectedCommitCount);
			activeHeadAfter.Should().Be(activeHeadBefore);
		}
		finally
		{
			if (Directory.Exists(databaseDirectory))
			{
				Directory.Delete(databaseDirectory, true);
			}
		}
	}
}
