using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Storage;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Tests.Storage;

public sealed class StatisticsSqliteDatabaseTests
{
	[Fact]
	public async Task PersistScanAsync_CreatesSchemaAndAggregatesProjectMeasurements()
	{
		await using var fixture = new DatabaseFixture();
		var definition = new StatisticsScanDefinition("Release", "net10.0", false, "1");
		var snapshot = CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 2);

		var stored = await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(definition, snapshot), TestContext.Current.CancellationToken);
		var summary = await fixture.Database.ReadLatestSummaryAsync(TestContext.Current.CancellationToken);

		stored.Status.Should().Be(StatisticsScanStatus.Complete);
		summary.Should().NotBeNull();
		summary!.ProjectCount.Should().Be(1);
		summary.FailureCount.Should().Be(0);
		summary.Measurements.Should().ContainSingle(measurement => measurement.Dimension == StatisticsDimension.TypeKind && measurement.Bucket == "Class" && measurement.Count == 2);
		summary.GroupedMeasurements.Should().ContainSingle(measurement => measurement.Dimension == StatisticsDimension.TypeKind
			&& measurement.Bucket == "Class"
			&& measurement.GroupDimension == StatisticsDimension.TypeAccessibility
			&& measurement.GroupBucket == "Public"
			&& measurement.Count == 2);
	}

	[Fact]
	public async Task PersistScanAsync_ReplacesMatchingCommitScanAndSupportsTrendAndComparison()
	{
		await using var fixture = new DatabaseFixture();
		var definition = new StatisticsScanDefinition("Release", null, false, "1");
		var repository = new StatisticsRepositoryIdentity("D:\\repo\\Pizza", "D:\\repo\\Pizza\\.git");
		var firstCommit = new StatisticsGitCommit("aaa", "tree-a", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), []);
		var secondCommit = new StatisticsGitCommit("bbb", "tree-b", DateTimeOffset.Parse("2026-01-02T00:00:00Z"), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), ["aaa"]);

		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 1), repository, firstCommit), TestContext.Current.CancellationToken);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 3), repository, secondCommit), TestContext.Current.CancellationToken);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 2), repository, firstCommit), TestContext.Current.CancellationToken);

		var existing = await fixture.Database.FindCommitScanAsync(definition, repository, "aaa", "D:\\repo\\Pizza", TestContext.Current.CancellationToken);
		var trend = await fixture.Database.ReadTrendAsync(StatisticsDimension.TypeKind, "Class", TestContext.Current.CancellationToken);
		var comparison = await fixture.Database.CompareCommitsAsync("aaa", "bbb", TestContext.Current.CancellationToken);
		var changes = await fixture.Database.ReadCommitChangesAsync(StatisticsDimension.TypeKind, "Class", TestContext.Current.CancellationToken);

		existing.Should().NotBeNull();
		trend.Should().HaveCount(2);
		trend.Select(point => point.Count).Should().ContainInOrder(2, 3);
		comparison.Should().ContainSingle(item => item.Dimension == StatisticsDimension.TypeKind && item.Bucket == "Class" && item.FromCount == 2 && item.ToCount == 3 && item.Delta == 1);
		changes.Should().HaveCount(2);
		changes.Select(change => change.Count).Should().ContainInOrder(2, 3);
		changes.Last().Delta.Should().Be(1);
	}

	[Fact]
	public async Task PersistScanAsync_PersistsPartialFailures()
	{
		await using var fixture = new DatabaseFixture();
		var snapshot = new StatisticsScanSnapshot(
			"D:\\repo\\Pizza",
			StatisticsScanStatus.Partial,
			[CreateProjectSnapshot(1)],
			[new StatisticsScanFailure("D:\\repo\\Pizza\\Pizza.csproj", "Compiler", "One compiler error was observed.")]);

		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(new StatisticsScanDefinition("Release", null, false, "1"), snapshot), TestContext.Current.CancellationToken);
		var summary = await fixture.Database.ReadLatestSummaryAsync(TestContext.Current.CancellationToken);

		summary!.Scan.Status.Should().Be(StatisticsScanStatus.Partial);
		summary.FailureCount.Should().Be(1);
	}

	[Fact]
	public async Task ReadHistoryQueries_UseTheMostRecentlyStoredHistoryDefinition()
	{
		await using var fixture = new DatabaseFixture();
		var olderDefinition = new StatisticsScanDefinition("Debug", null, false, "1");
		var latestDefinition = new StatisticsScanDefinition("Release", null, false, "1");
		var repository = new StatisticsRepositoryIdentity("D:\\repo\\Pizza", "D:\\repo\\Pizza\\.git");
		var firstCommit = new StatisticsGitCommit("aaa", "tree-a", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), []);
		var secondCommit = new StatisticsGitCommit("bbb", "tree-b", DateTimeOffset.Parse("2026-01-02T00:00:00Z"), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), ["aaa"]);

		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(olderDefinition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 20), repository, firstCommit), TestContext.Current.CancellationToken);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(olderDefinition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 30), repository, secondCommit), TestContext.Current.CancellationToken);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(latestDefinition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 2), repository, firstCommit), TestContext.Current.CancellationToken);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(latestDefinition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 3), repository, secondCommit), TestContext.Current.CancellationToken);

		var trend = await fixture.Database.ReadTrendAsync(StatisticsDimension.TypeKind, "Class", TestContext.Current.CancellationToken);
		var comparison = await fixture.Database.CompareCommitsAsync("aaa", "bbb", TestContext.Current.CancellationToken);
		var changes = await fixture.Database.ReadCommitChangesAsync(StatisticsDimension.TypeKind, "Class", TestContext.Current.CancellationToken);

		trend.Select(point => point.Count).Should().ContainInOrder(2, 3);
		comparison.Should().ContainSingle(item => item.Dimension == StatisticsDimension.TypeKind && item.Bucket == "Class" && item.FromCount == 2 && item.ToCount == 3);
		changes.Select(change => change.Count).Should().ContainInOrder(2, 3);
	}

	[Fact]
	public async Task ReadTrendAsync_IncludesZeroWhenABucketIsAbsentFromACommit()
	{
		await using var fixture = new DatabaseFixture();
		var definition = new StatisticsScanDefinition("Release", null, false, "1");
		var repository = new StatisticsRepositoryIdentity("D:\\repo\\Pizza", "D:\\repo\\Pizza\\.git");
		var firstCommit = new StatisticsGitCommit("aaa", "tree-a", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), []);
		var secondCommit = new StatisticsGitCommit("bbb", "tree-b", DateTimeOffset.Parse("2026-01-02T00:00:00Z"), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), [firstCommit.Sha]);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, 1), repository, firstCommit), TestContext.Current.CancellationToken);
		await fixture.Database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateSnapshotWithoutClass("D:\\repo\\Pizza"), repository, secondCommit), TestContext.Current.CancellationToken);

		var trend = await fixture.Database.ReadTrendAsync(StatisticsDimension.TypeKind, "Class", TestContext.Current.CancellationToken);

		trend.Select(point => point.Count).Should().ContainInOrder(1, 0);
	}

	private static StatisticsScanSnapshot CreateSnapshot(string inputPath, StatisticsScanStatus status, long classCount)
	{
		var result = new StatisticsScanSnapshot(inputPath, status, [CreateProjectSnapshot(classCount)], []);

		return result;
	}

	private static StatisticsProjectSnapshot CreateProjectSnapshot(long classCount)
	{
		var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");
		var measurements = new[]
		{
			new StatisticsMeasurement(StatisticsDimension.TypeKind, "Class", classCount),
			new StatisticsMeasurement(StatisticsDimension.DependencySite, "Method", classCount)
		};
		var groupedMeasurements = new[]
		{
			new StatisticsGroupedMeasurement(StatisticsDimension.TypeKind, "Class", StatisticsDimension.TypeAccessibility, "Public", classCount)
		};
		var result = new StatisticsProjectSnapshot(identity, measurements, 1, checked((int)classCount), 1, 0, 0, groupedMeasurements);

		return result;
	}

	private static StatisticsScanSnapshot CreateSnapshotWithoutClass(string inputPath)
	{
		var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");
		var measurements = new[] { new StatisticsMeasurement(StatisticsDimension.DependencySite, "Method", 1) };
		var project = new StatisticsProjectSnapshot(identity, measurements, 1, 0, 1, 0, 0);
		var result = new StatisticsScanSnapshot(inputPath, StatisticsScanStatus.Complete, [project], []);

		return result;
	}

	private sealed class DatabaseFixture : IAsyncDisposable
	{
		private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", Guid.NewGuid().ToString("N"));

		public DatabaseFixture()
		{
			Directory.CreateDirectory(_directoryPath);
			Database = new StatisticsSqliteDatabase(Path.Combine(_directoryPath, "statistics.db"));
		}

		public StatisticsSqliteDatabase Database { get; }

		public ValueTask DisposeAsync()
		{
			if (Directory.Exists(_directoryPath))
			{
				Directory.Delete(_directoryPath, true);
			}

			return ValueTask.CompletedTask;
		}
	}
}
