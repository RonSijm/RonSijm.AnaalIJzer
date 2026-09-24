using Microsoft.Data.Sqlite;
using RonSijm.Anaaltomy.Charting.Model;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.Anaaltomy.Charting.Rendering;
using RonSijm.AnaalIJzer.Statistics.GitHistory.Git;
using RonSijm.AnaalIJzer.Statistics.GitHistory.Scanning;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Storage;
using RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;
using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.Anaaltomy.CommandLine;

internal static class AnaaltomyCommandLine
{
	private const string CollectorVersion = "1";

	public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
	{
		try
		{
			var options = AnaaltomyArgumentParser.Parse(args);
			if (options.Command is AnaaltomyCommand.Scan or AnaaltomyCommand.History)
			{
				WorkspaceBuildEnvironment.Initialize();
			}

			var result = options.Command switch
			{
				AnaaltomyCommand.Help => WriteHelp(output),
				AnaaltomyCommand.Scan => await RunScanAsync(options, output, cancellationToken),
				AnaaltomyCommand.History => await RunHistoryAsync(options, output, cancellationToken),
				AnaaltomyCommand.Summary => await RunSummaryAsync(options, output, cancellationToken),
				AnaaltomyCommand.Trend => await RunTrendAsync(options, output, cancellationToken),
				AnaaltomyCommand.Compare => await RunCompareAsync(options, output, cancellationToken),
				AnaaltomyCommand.Commits => await RunCommitChangesAsync(options, output, cancellationToken),
				AnaaltomyCommand.Export => await RunExportAsync(options, output, cancellationToken),
				AnaaltomyCommand.ExportDatabase => await RunExportDatabaseAsync(options, output, cancellationToken),
				AnaaltomyCommand.Chart => await RunChartAsync(options, output, cancellationToken),
				_ => (int)AnaaltomyExitCode.InvalidInput
			};

			return result;
		}
		catch (SqliteException exception)
		{
			await error.WriteLineAsync("Database failure: " + exception.Message);

			return (int)AnaaltomyExitCode.PersistenceFailure;
		}
		catch (ArgumentException exception)
		{
			await error.WriteLineAsync("Invalid input: " + exception.Message);
			await error.WriteLineAsync("Run 'anaaltomy --help' for usage.");

			return (int)AnaaltomyExitCode.InvalidInput;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception exception)
		{
			await error.WriteLineAsync("Anaaltomy could not complete the operation: " + exception.Message);

			return (int)AnaaltomyExitCode.Partial;
		}
	}

	private static async Task<int> RunScanAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var (inputKind, inputPath) = GetScanInput(options);
		var definition = CreateDefinition(options);
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var request = new StatisticsWorkspaceScanRequest(
			inputKind,
			inputPath,
			definition.Configuration,
			definition.TargetFramework,
			definition.IncludeGeneratedCode,
			ParseRestoreMode(options.GetValue("--restore-mode") ?? "auto"));
		var scanner = new StatisticsWorkspaceScanner();
		var snapshot = await scanner.ScanAsync(request, cancellationToken);
		await database.PersistScanAsync(new StatisticsPersistRequest(definition, snapshot), cancellationToken);
		var summary = await database.ReadLatestSummaryAsync(cancellationToken) ?? throw new InvalidOperationException("The database did not return the persisted scan.");
		AnaaltomyOutput.WriteSummary(output, summary);
		AnaaltomyOutput.WriteFailures(output, snapshot.Failures);
		var result = snapshot.Status == StatisticsScanStatus.Complete || options.HasFlag("--allow-partial")
			? (int)AnaaltomyExitCode.Success
			: (int)AnaaltomyExitCode.Partial;

		return result;
	}

	private static async Task<int> RunHistoryAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var definition = CreateDefinition(options);
		var selection = new GitHistorySelection(
			options.HasFlag("--from-root"),
			options.GetValue("--from"),
			options.GetValue("--to"),
			options.HasFlag("--first-parent"),
			ParseOptionalPositiveInt(options.GetValue("--max-commits"), "--max-commits"),
			options.HasFlag("--include-commit-metadata"));
		var runner = new GitCommandRunner();
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var scanner = new StatisticsGitHistoryScanner(
			new GitRepositoryReader(runner),
			runner,
			new StatisticsWorkspaceScanner(),
			database);
		var request = new StatisticsGitHistoryScanRequest(
			RequireValue(options, "--repository"),
			definition,
			selection,
			options.HasFlag("--resume"),
			ParseRestoreMode(options.GetValue("--restore-mode") ?? "always"));
		var progress = new Progress<string>(message => output.WriteLine(message));
		var result = await scanner.ScanAsync(request, progress, cancellationToken);
		await output.WriteLineAsync("History scan selected " + result.SelectedCommitCount + " commit(s), scanned " + result.ScannedCommitCount + ", and skipped " + result.SkippedCompleteCommitCount + " complete commit(s).");
		var exitCode = result.IsComplete || options.HasFlag("--allow-partial")
			? (int)AnaaltomyExitCode.Success
			: (int)AnaaltomyExitCode.Partial;

		return exitCode;
	}

	private static async Task<int> RunSummaryAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var summary = await database.ReadLatestSummaryAsync(cancellationToken) ?? throw new ArgumentException("The database does not contain a scan yet.");
		AnaaltomyOutput.WriteSummary(output, summary);

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<int> RunTrendAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var dimension = ParseDimension(RequireValue(options, "--dimension"));
		var bucket = RequireValue(options, "--bucket");
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var trend = await database.ReadTrendAsync(dimension, bucket, cancellationToken);
		AnaaltomyOutput.WriteTrend(output, trend);

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<int> RunCompareAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var comparison = await database.CompareCommitsAsync(RequireValue(options, "--from"), RequireValue(options, "--to"), cancellationToken);
		AnaaltomyOutput.WriteComparison(output, comparison);

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<int> RunCommitChangesAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var dimension = ParseDimension(RequireValue(options, "--dimension"));
		var bucket = RequireValue(options, "--bucket");
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var changes = await database.ReadCommitChangesAsync(dimension, bucket, cancellationToken);
		AnaaltomyOutput.WriteCommitChanges(output, changes);

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<int> RunExportAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var summary = await database.ReadLatestSummaryAsync(cancellationToken) ?? throw new ArgumentException("The database does not contain a scan yet.");
		var outputPath = RequireValue(options, "--output");
		await AnaaltomyOutput.WriteExportAsync(RequireValue(options, "--format"), outputPath, summary, cancellationToken);
		await output.WriteLineAsync("Exported statistics to " + Path.GetFullPath(outputPath) + ".");

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<int> RunExportDatabaseAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var databasePath = RequireValue(options, "--database");
		var format = RequireValue(options, "--format");
		var outputDirectory = RequireValue(options, "--output-directory");
		var exportedFiles = await AnaaltomyDatabaseExporter.ExportAsync(databasePath, format, outputDirectory, cancellationToken);
		await output.WriteLineAsync("Exported database to " + Path.GetFullPath(outputDirectory) + ".");
		foreach (var exportedFile in exportedFiles)
		{
			await output.WriteLineAsync("Exported " + exportedFile + ".");
		}

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<int> RunChartAsync(AnaaltomyOptions options, TextWriter output, CancellationToken cancellationToken)
	{
		var database = new StatisticsSqliteDatabase(RequireValue(options, "--database"));
		var generator = new StatisticsChartReportGenerator();
		var outputDirectory = RequireValue(options, "--output-directory");
		var report = options.HasFlag("--trend")
			? await GenerateTrendChartAsync(options, database, generator, outputDirectory, cancellationToken)
			: await GenerateBreakdownChartsAsync(options, database, generator, outputDirectory, cancellationToken);
		foreach (var outputPath in report.OutputPaths)
		{
			await output.WriteLineAsync("Generated chart " + outputPath + ".");
		}

		return (int)AnaaltomyExitCode.Success;
	}

	private static async Task<StatisticsChartReport> GenerateBreakdownChartsAsync(AnaaltomyOptions options, StatisticsSqliteDatabase database, StatisticsChartReportGenerator generator, string outputDirectory, CancellationToken cancellationToken)
	{
		if (options.GetValue("--bucket") is not null)
		{
			throw new ArgumentException("--bucket is only valid with chart --trend.");
		}

		var summary = await database.ReadLatestSummaryAsync(cancellationToken) ?? throw new ArgumentException("The database does not contain a scan yet.");
		StatisticsDimension? requestedDimension = options.GetValue("--dimension") is { } dimensionValue
			? ParseDimension(dimensionValue)
			: null;
		StatisticsDimension? requestedGroupDimension = options.GetValue("--group-by") is { } groupDimensionValue
			? ParseDimension(groupDimensionValue)
			: null;
		var subject = GetChartSubject(summary.Scan.InputPath);
		var result = options.HasFlag("--group") || requestedGroupDimension is not null
			? generator.GenerateGrouped(outputDirectory, summary.GroupedMeasurements, requestedDimension, requestedGroupDimension, subject)
			: generator.Generate(outputDirectory, summary.Measurements, requestedDimension, subject);

		return result;
	}

	private static async Task<StatisticsChartReport> GenerateTrendChartAsync(AnaaltomyOptions options, StatisticsSqliteDatabase database, StatisticsChartReportGenerator generator, string outputDirectory, CancellationToken cancellationToken)
	{
		if (options.HasFlag("--group") || options.GetValue("--group-by") is not null)
		{
			throw new ArgumentException("--group and --group-by are only valid for latest-scan breakdown charts.");
		}

		var dimension = ParseDimension(RequireValue(options, "--dimension"));
		var bucket = RequireValue(options, "--bucket");
		var trend = await database.ReadTrendAsync(dimension, bucket, cancellationToken);
		var points = trend
			.Select(point => new StatisticsTrendChartPoint(point.CommitSha, point.CommittedAtUtc, point.Count))
			.ToArray();
		var result = generator.GenerateTrend(outputDirectory, dimension, bucket, points);

		return result;
	}

	private static string GetChartSubject(string inputPath)
	{
		var fullPath = Path.GetFullPath(inputPath);
		var extension = Path.GetExtension(fullPath);
		var isProjectOrSolutionFile = extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
			|| extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
		var result = isProjectOrSolutionFile
			? Path.GetFileNameWithoutExtension(fullPath)
			: Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (string.IsNullOrWhiteSpace(result))
		{
			result = fullPath;
		}

		return result;
	}

	private static (StatisticsWorkspaceInputKind InputKind, string InputPath) GetScanInput(AnaaltomyOptions options)
	{
		var inputs = new (StatisticsWorkspaceInputKind InputKind, string? InputPath)[]
		{
			(StatisticsWorkspaceInputKind.Project, options.GetValue("--project")),
			(StatisticsWorkspaceInputKind.Solution, options.GetValue("--solution")),
			(StatisticsWorkspaceInputKind.Directory, options.GetValue("--directory"))
		}.Where(input => !string.IsNullOrWhiteSpace(input.InputPath)).ToArray();
		if (inputs.Length != 1)
		{
			throw new ArgumentException("scan requires exactly one of --project, --solution, or --directory.");
		}

		var result = (inputs[0].InputKind, inputs[0].InputPath!);

		return result;
	}

	private static StatisticsScanDefinition CreateDefinition(AnaaltomyOptions options)
	{
		var result = new StatisticsScanDefinition(
			options.GetValue("--configuration") ?? "Release",
			options.GetValue("--framework"),
			options.HasFlag("--include-generated"),
			CollectorVersion);

		return result;
	}

	private static string RequireValue(AnaaltomyOptions options, string name)
	{
		var value = options.GetValue(name);
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Missing required option: " + name);
		}

		var result = value;

		return result;
	}

	private static WorkspaceRestoreMode ParseRestoreMode(string value)
	{
		var result = value.ToLowerInvariant() switch
		{
			"auto" => WorkspaceRestoreMode.Auto,
			"never" => WorkspaceRestoreMode.Never,
			"always" => WorkspaceRestoreMode.Always,
			_ => throw new ArgumentException("--restore-mode must be auto, never, or always.")
		};

		return result;
	}

	private static StatisticsDimension ParseDimension(string value)
	{
		if (!Enum.TryParse<StatisticsDimension>(value, true, out var result))
		{
			throw new ArgumentException("Unknown statistics dimension: " + value);
		}

		return result;
	}

	private static int? ParseOptionalPositiveInt(string? value, string optionName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (!int.TryParse(value, out var result) || result <= 0)
		{
			throw new ArgumentException(optionName + " must be a positive integer.");
		}

		return result;
	}

	private static int WriteHelp(TextWriter output)
	{
		output.WriteLine("Anaaltomy measures compiled C# code structure and stores results in SQLite.");
		output.WriteLine();
		output.WriteLine("anaaltomy scan --project <project.csproj> --database <statistics.db>");
		output.WriteLine("anaaltomy scan --solution <solution.slnx> --database <statistics.db>");
		output.WriteLine("anaaltomy scan --directory <directory> --database <statistics.db>");
		output.WriteLine("anaaltomy history --repository <directory> --from-root --database <statistics.db>");
		output.WriteLine("anaaltomy history --repository <directory> --from <revision> --to <revision> --database <statistics.db>");
		output.WriteLine("anaaltomy summary --database <statistics.db>");
		output.WriteLine("anaaltomy trend --database <statistics.db> --dimension DependencySite --bucket Local");
		output.WriteLine("anaaltomy compare --database <statistics.db> --from <revision> --to <revision>");
		output.WriteLine("anaaltomy commits --database <statistics.db> --dimension DependencySite --bucket Local");
		output.WriteLine("anaaltomy export --database <statistics.db> --format json|csv|markdown --output <statistics.json>");
		output.WriteLine("anaaltomy export-database --database <statistics.db> --format json|csv|markdown --output-directory <directory>");
		output.WriteLine("anaaltomy chart --database <statistics.db> --output-directory <chart-directory> [--dimension DependencySite]");
		output.WriteLine("anaaltomy chart --database <statistics.db> --output-directory <chart-directory> --group [--dimension MemberAccessibility] [--group-by MemberKind]");
		output.WriteLine("anaaltomy chart --database <statistics.db> --output-directory <chart-directory> --trend --dimension DependencySite --bucket Local");
		output.WriteLine();
		output.WriteLine("scan/history options: --configuration Release --framework net10.0 --include-generated --restore-mode auto|never|always --allow-partial");
		output.WriteLine("history options: --first-parent --max-commits <count> --resume --include-commit-metadata");

		return (int)AnaaltomyExitCode.Success;
	}
}
