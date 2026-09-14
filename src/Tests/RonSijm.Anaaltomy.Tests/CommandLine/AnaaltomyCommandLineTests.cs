using RonSijm.Anaaltomy.CommandLine;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Storage;

namespace RonSijm.Anaaltomy.Tests.CommandLine;

public sealed class AnaaltomyCommandLineTests
{
	[Fact]
	public async Task RunAsync_HelpWritesCommandReference()
	{
		using var output = new StringWriter();
		using var error = new StringWriter();

		var exitCode = await AnaaltomyCommandLine.RunAsync(["--help"], output, error, TestContext.Current.CancellationToken);

		exitCode.Should().Be((int)AnaaltomyExitCode.Success);
		output.ToString().Should().Contain("anaaltomy scan");
		error.ToString().Should().BeEmpty();
	}

	[Fact]
	public async Task RunAsync_ScanPersistsChartsAndPrintsSummary()
	{
		var directoryPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directoryPath);
		try
		{
			var projectPath = Path.Combine(GetRepositoryRoot(), "src", "Main", "RonSijm.AnaalIJzer.Core.Statistics", "RonSijm.AnaalIJzer.Core.Statistics.csproj");
			var databasePath = Path.Combine(directoryPath, "statistics.db");
			var chartDirectoryPath = Path.Combine(directoryPath, "charts");
			using var output = new StringWriter();
			using var error = new StringWriter();

			var exitCode = await AnaaltomyCommandLine.RunAsync(["scan", "--project", projectPath, "--database", databasePath, "--restore-mode", "never"], output, error, TestContext.Current.CancellationToken);

			exitCode.Should().Be((int)AnaaltomyExitCode.Success);
			File.Exists(databasePath).Should().BeTrue();
			output.ToString().Should().Contain("TypeKind");
			error.ToString().Should().BeEmpty();

			output.GetStringBuilder().Clear();
			var chartExitCode = await AnaaltomyCommandLine.RunAsync(["chart", "--database", databasePath, "--output-directory", chartDirectoryPath, "--dimension", "TypeKind"], output, error, TestContext.Current.CancellationToken);

			chartExitCode.Should().Be((int)AnaaltomyExitCode.Success);
			File.Exists(Path.Combine(chartDirectoryPath, "type-kinds.png")).Should().BeTrue();
			output.ToString().Should().Contain("Generated chart");

			output.GetStringBuilder().Clear();
			var groupedChartExitCode = await AnaaltomyCommandLine.RunAsync(["chart", "--database", databasePath, "--output-directory", chartDirectoryPath, "--group", "--dimension", "TypeKind", "--group-by", "TypeAccessibility"], output, error, TestContext.Current.CancellationToken);

			groupedChartExitCode.Should().Be((int)AnaaltomyExitCode.Success);
			File.Exists(Path.Combine(chartDirectoryPath, "type-kinds-by-type-accessibility.png")).Should().BeTrue();
		}
		finally
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, true);
			}
		}
	}

	[Fact]
	public async Task RunAsync_ChartTrendWritesPngForStoredHistory()
	{
		var directoryPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directoryPath);
		try
		{
			var databasePath = Path.Combine(directoryPath, "statistics.db");
			var chartDirectoryPath = Path.Combine(directoryPath, "charts");
			await PersistHistoryAsync(databasePath);
			using var output = new StringWriter();
			using var error = new StringWriter();

			var exitCode = await AnaaltomyCommandLine.RunAsync(["chart", "--database", databasePath, "--output-directory", chartDirectoryPath, "--trend", "--dimension", "TypeKind", "--bucket", "Class"], output, error, TestContext.Current.CancellationToken);

			exitCode.Should().Be((int)AnaaltomyExitCode.Success);
			File.Exists(Path.Combine(chartDirectoryPath, "type-kinds-class-trend.png")).Should().BeTrue();
			output.ToString().Should().Contain("Generated chart");
			error.ToString().Should().BeEmpty();
		}
		finally
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, true);
			}
		}
	}

	[Fact]
	public async Task RunAsync_ChartGroupedWritesPngForStoredGroupedMeasurements()
	{
		var directoryPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directoryPath);
		try
		{
			var databasePath = Path.Combine(directoryPath, "statistics.db");
			var chartDirectoryPath = Path.Combine(directoryPath, "charts");
			var database = new StatisticsSqliteDatabase(databasePath);
			var definition = new StatisticsScanDefinition("Release", "net10.0", false, "1");
			var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");
			var project = new StatisticsProjectSnapshot(
				identity,
				[new StatisticsMeasurement(StatisticsDimension.MemberAccessibility, "Public", 3)],
				1,
				1,
				3,
				0,
				0,
				[new StatisticsGroupedMeasurement(StatisticsDimension.MemberAccessibility, "Public", StatisticsDimension.MemberKind, "Method", 3)]);
			var snapshot = new StatisticsScanSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, [project], []);
			await database.PersistScanAsync(new StatisticsPersistRequest(definition, snapshot), TestContext.Current.CancellationToken);
			using var output = new StringWriter();
			using var error = new StringWriter();

			var exitCode = await AnaaltomyCommandLine.RunAsync(["chart", "--database", databasePath, "--output-directory", chartDirectoryPath, "--group", "--dimension", "MemberAccessibility", "--group-by", "MemberKind"], output, error, TestContext.Current.CancellationToken);

			exitCode.Should().Be((int)AnaaltomyExitCode.Success);
			File.Exists(Path.Combine(chartDirectoryPath, "member-accessibility-by-member-kinds.png")).Should().BeTrue();
			output.ToString().Should().Contain("Generated chart");
			error.ToString().Should().BeEmpty();
		}
		finally
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, true);
			}
		}
	}

	[Fact]
	public async Task RunAsync_ChartTrendRequiresDimensionAndBucket()
	{
		using var output = new StringWriter();
		using var error = new StringWriter();

		var exitCode = await AnaaltomyCommandLine.RunAsync(["chart", "--database", "statistics.db", "--output-directory", "charts", "--trend", "--dimension", "TypeKind"], output, error, TestContext.Current.CancellationToken);

		exitCode.Should().Be((int)AnaaltomyExitCode.InvalidInput);
		error.ToString().Should().Contain("Missing required option: --bucket");
	}

	[Fact]
	public async Task RunAsync_HistoryRequiresAnExplicitHistorySelection()
	{
		using var output = new StringWriter();
		using var error = new StringWriter();

		var exitCode = await AnaaltomyCommandLine.RunAsync(["history", "--repository", Directory.GetCurrentDirectory(), "--database", "statistics.db"], output, error, TestContext.Current.CancellationToken);

		exitCode.Should().Be((int)AnaaltomyExitCode.InvalidInput);
		error.ToString().Should().Contain("--from");
	}

	[Fact]
	public void WriteFailures_WritesEachRecordedFailure()
	{
		using var output = new StringWriter();
		var failures = new[]
		{
			new StatisticsScanFailure("C:\\Projects\\Pizza\\Pizza.csproj", "Compiler", "1 compiler error(s) were observed."),
			new StatisticsScanFailure(null, "Workspace", "The workspace could not evaluate a project.")
		};

		AnaaltomyOutput.WriteFailures(output, failures);

		output.ToString().Should().Contain("Failures:");
		output.ToString().Should().Contain("[Compiler] (C:\\Projects\\Pizza\\Pizza.csproj) 1 compiler error(s) were observed.");
		output.ToString().Should().Contain("[Workspace] The workspace could not evaluate a project.");
	}

	private static string GetRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			var projectPath = Path.Combine(directory.FullName, "src", "Main", "RonSijm.AnaalIJzer.Core.Statistics", "RonSijm.AnaalIJzer.Core.Statistics.csproj");
			if (File.Exists(projectPath))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
	}

	private static async Task PersistHistoryAsync(string databasePath)
	{
		var database = new StatisticsSqliteDatabase(databasePath);
		var definition = new StatisticsScanDefinition("Release", "net10.0", false, "1");
		var repository = new StatisticsRepositoryIdentity("D:\\repo\\Pizza", "D:\\repo\\Pizza\\.git");
		var firstCommit = new StatisticsGitCommit("aaa11111", "tree-a", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"), []);
		var secondCommit = new StatisticsGitCommit("bbb22222", "tree-b", DateTimeOffset.Parse("2026-01-02T00:00:00Z"), DateTimeOffset.Parse("2026-01-02T00:00:00Z"), [firstCommit.Sha]);
		await database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateHistorySnapshot(2), repository, firstCommit), TestContext.Current.CancellationToken);
		await database.PersistScanAsync(new StatisticsPersistRequest(definition, CreateHistorySnapshot(3), repository, secondCommit), TestContext.Current.CancellationToken);
	}

	private static StatisticsScanSnapshot CreateHistorySnapshot(long classCount)
	{
		var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");
		var measurements = new[] { new StatisticsMeasurement(StatisticsDimension.TypeKind, "Class", classCount) };
		var project = new StatisticsProjectSnapshot(identity, measurements, 1, checked((int)classCount), 1, 0, 0);
		var result = new StatisticsScanSnapshot("D:\\repo\\Pizza", StatisticsScanStatus.Complete, [project], []);

		return result;
	}
}
