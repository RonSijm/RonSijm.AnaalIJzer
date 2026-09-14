using Microsoft.Data.Sqlite;
using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Storage;

public sealed class StatisticsSqliteDatabase
{
	private readonly string _databasePath;

	public StatisticsSqliteDatabase(string databasePath)
	{
		if (string.IsNullOrWhiteSpace(databasePath))
		{
			throw new ArgumentException("A SQLite database path is required.", nameof(databasePath));
		}

		_databasePath = string.Equals(databasePath, ":memory:", StringComparison.Ordinal) ? databasePath : Path.GetFullPath(databasePath);
	}

	public string DatabasePath => _databasePath;

	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		if (!string.Equals(_databasePath, ":memory:", StringComparison.Ordinal))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
		}

		await using var connection = await OpenConnectionAsync(cancellationToken);
		await ExecuteNonQueryAsync(connection, null, StatisticsDatabaseSchema.VersionOneStatements[0], cancellationToken);
		var currentVersion = await ReadCurrentSchemaVersionAsync(connection, null, cancellationToken);
		if (currentVersion >= StatisticsDatabaseSchema.LatestVersion)
		{
			return;
		}

		await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
		if (currentVersion < 1)
		{
			for (var index = 1; index < StatisticsDatabaseSchema.VersionOneStatements.Length; index++)
			{
				await ExecuteNonQueryAsync(connection, transaction, StatisticsDatabaseSchema.VersionOneStatements[index], cancellationToken);
			}

			await RecordSchemaVersionAsync(connection, transaction, 1, cancellationToken);
			currentVersion = 1;
		}

		if (currentVersion < 2)
		{
			foreach (var statement in StatisticsDatabaseSchema.VersionTwoStatements)
			{
				await ExecuteNonQueryAsync(connection, transaction, statement, cancellationToken);
			}

			await RecordSchemaVersionAsync(connection, transaction, 2, cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);
	}

	public async Task<StatisticsStoredScan> PersistScanAsync(StatisticsPersistRequest request, CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
		var definitionId = await GetOrCreateScanDefinitionAsync(connection, transaction, request.Definition, cancellationToken);
		long? repositoryId = null;
		if (request.Repository is not null)
		{
			repositoryId = await GetOrCreateRepositoryAsync(connection, transaction, request.Repository, cancellationToken);
		}

		if (request.Commit is not null)
		{
			if (repositoryId is null)
			{
				throw new InvalidOperationException("A Git commit requires a repository identity.");
			}

			await UpsertGitCommitAsync(connection, transaction, repositoryId.Value, request.Commit, cancellationToken);
		}

		await DeleteExistingScanAsync(connection, transaction, definitionId, repositoryId, request.Commit?.Sha, request.Snapshot.InputPath, cancellationToken);
		var scanId = await InsertCommitScanAsync(connection, transaction, definitionId, repositoryId, request.Commit?.Sha, request.Snapshot, cancellationToken);
		var projectIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
		foreach (var project in request.Snapshot.Projects)
		{
			var projectId = await InsertProjectScanAsync(connection, transaction, scanId, project, cancellationToken);
			projectIds[NormalizeProjectPath(request.Snapshot.InputPath, project.Identity.ProjectPath)] = projectId;
		}

		foreach (var failure in request.Snapshot.Failures)
		{
			long? projectId = null;
			if (!string.IsNullOrWhiteSpace(failure.ProjectPath))
			{
				projectIds.TryGetValue(NormalizeProjectPath(request.Snapshot.InputPath, failure.ProjectPath), out var resolvedProjectId);
				projectId = resolvedProjectId == 0 ? null : resolvedProjectId;
			}

			await InsertFailureAsync(connection, transaction, scanId, projectId, failure, cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);
		var result = new StatisticsStoredScan(scanId, definitionId, repositoryId, request.Commit?.Sha, request.Snapshot.InputPath, request.Snapshot.Status);

		return result;
	}

	public async Task PersistGitCommitsAsync(StatisticsRepositoryIdentity repository, IReadOnlyList<StatisticsGitCommit> commits, CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
		var repositoryId = await GetOrCreateRepositoryAsync(connection, transaction, repository, cancellationToken);
		foreach (var commit in commits)
		{
			await UpsertGitCommitAsync(connection, transaction, repositoryId, commit, cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);
	}

	public async Task<StatisticsStoredScan?> FindCommitScanAsync(StatisticsScanDefinition definition, StatisticsRepositoryIdentity repository, string commitSha, string inputPath, CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		var definitionId = await FindScanDefinitionIdAsync(connection, null, definition.GetOptionsHash(), cancellationToken);
		if (definitionId is null)
		{
			return null;
		}

		var repositoryId = await FindRepositoryIdAsync(connection, null, repository.CanonicalPath, cancellationToken);
		if (repositoryId is null)
		{
			return null;
		}

		var result = await FindStoredScanAsync(connection, null, definitionId.Value, repositoryId, commitSha, inputPath, cancellationToken);

		return result;
	}

	public async Task<StatisticsSummary?> ReadLatestSummaryAsync(CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		const string sql = """
			SELECT CommitScanId, ScanDefinitionId, RepositoryId, CommitSha, InputPath, Status
			FROM CommitScan
			ORDER BY CompletedAtUtc DESC, CommitScanId DESC
			LIMIT 1;
			""";
		await using var command = CreateCommand(connection, null, sql);
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return null;
		}

		var scan = ReadStoredScan(reader);
		var result = await ReadSummaryAsync(connection, scan, cancellationToken);

		return result;
	}

	public async Task<IReadOnlyList<StatisticsTrendPoint>> ReadTrendAsync(StatisticsDimension dimension, string bucket, CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		const string sql = """
			WITH LatestHistory AS (
				SELECT ScanDefinitionId, RepositoryId
				FROM CommitScan
				WHERE CommitSha IS NOT NULL
				ORDER BY CompletedAtUtc DESC, CommitScanId DESC
				LIMIT 1
			)
			SELECT CommitScan.CommitSha, GitCommit.CommittedAtUtc, COALESCE(SUM(Measurement.Count), 0)
			FROM CommitScan
			JOIN ProjectScan ON ProjectScan.CommitScanId = CommitScan.CommitScanId
			LEFT JOIN Measurement ON Measurement.ProjectScanId = ProjectScan.ProjectScanId
				AND Measurement.Dimension = @dimension
				AND Measurement.Bucket = @bucket
			JOIN GitCommit ON GitCommit.RepositoryId = CommitScan.RepositoryId AND GitCommit.Sha = CommitScan.CommitSha
			JOIN LatestHistory ON LatestHistory.ScanDefinitionId = CommitScan.ScanDefinitionId AND LatestHistory.RepositoryId = CommitScan.RepositoryId
			WHERE CommitScan.CommitSha IS NOT NULL
			GROUP BY CommitScan.CommitScanId, CommitScan.CommitSha, GitCommit.CommittedAtUtc
			ORDER BY GitCommit.CommittedAtUtc, CommitScan.CommitScanId;
			""";
		await using var command = CreateCommand(connection, null, sql, ("@dimension", dimension.ToString()), ("@bucket", bucket));
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var points = new List<StatisticsTrendPoint>();
		while (await reader.ReadAsync(cancellationToken))
		{
			points.Add(new StatisticsTrendPoint(
				reader.GetString(0),
				ReadNullableTimestamp(reader, 1),
				dimension,
				bucket,
				reader.GetInt64(2)));
		}

		var result = points;

		return result;
	}

	public async Task<IReadOnlyList<StatisticsComparison>> CompareCommitsAsync(string fromCommitSha, string toCommitSha, CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		const string sql = """
			WITH LatestHistory AS (
				SELECT ScanDefinitionId, RepositoryId
				FROM CommitScan
				WHERE CommitSha IS NOT NULL
				ORDER BY CompletedAtUtc DESC, CommitScanId DESC
				LIMIT 1
			), FromMeasurements AS (
				SELECT Measurement.Dimension, Measurement.Bucket, SUM(Measurement.Count) AS Count
				FROM CommitScan
				JOIN ProjectScan ON ProjectScan.CommitScanId = CommitScan.CommitScanId
				JOIN Measurement ON Measurement.ProjectScanId = ProjectScan.ProjectScanId
				JOIN LatestHistory ON LatestHistory.ScanDefinitionId = CommitScan.ScanDefinitionId AND LatestHistory.RepositoryId = CommitScan.RepositoryId
				WHERE CommitScan.CommitSha = @fromCommitSha
				GROUP BY Measurement.Dimension, Measurement.Bucket
			), ToMeasurements AS (
				SELECT Measurement.Dimension, Measurement.Bucket, SUM(Measurement.Count) AS Count
				FROM CommitScan
				JOIN ProjectScan ON ProjectScan.CommitScanId = CommitScan.CommitScanId
				JOIN Measurement ON Measurement.ProjectScanId = ProjectScan.ProjectScanId
				JOIN LatestHistory ON LatestHistory.ScanDefinitionId = CommitScan.ScanDefinitionId AND LatestHistory.RepositoryId = CommitScan.RepositoryId
				WHERE CommitScan.CommitSha = @toCommitSha
				GROUP BY Measurement.Dimension, Measurement.Bucket
			), Combined AS (
				SELECT Dimension, Bucket FROM FromMeasurements
				UNION
				SELECT Dimension, Bucket FROM ToMeasurements
			)
			SELECT Combined.Dimension, Combined.Bucket, COALESCE(FromMeasurements.Count, 0), COALESCE(ToMeasurements.Count, 0)
			FROM Combined
			LEFT JOIN FromMeasurements ON FromMeasurements.Dimension = Combined.Dimension AND FromMeasurements.Bucket = Combined.Bucket
			LEFT JOIN ToMeasurements ON ToMeasurements.Dimension = Combined.Dimension AND ToMeasurements.Bucket = Combined.Bucket
			ORDER BY Combined.Dimension, Combined.Bucket;
			""";
		await using var command = CreateCommand(connection, null, sql, ("@fromCommitSha", fromCommitSha), ("@toCommitSha", toCommitSha));
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var comparisons = new List<StatisticsComparison>();
		while (await reader.ReadAsync(cancellationToken))
		{
			if (!Enum.TryParse<StatisticsDimension>(reader.GetString(0), out var dimension))
			{
				continue;
			}

			comparisons.Add(new StatisticsComparison(dimension, reader.GetString(1), reader.GetInt64(2), reader.GetInt64(3)));
		}

		var result = comparisons;

		return result;
	}

	public async Task<IReadOnlyList<StatisticsCommitChange>> ReadCommitChangesAsync(StatisticsDimension dimension, string bucket, CancellationToken cancellationToken)
	{
		await InitializeAsync(cancellationToken);
		await using var connection = await OpenConnectionAsync(cancellationToken);
		const string sql = """
			WITH LatestHistory AS (
				SELECT ScanDefinitionId, RepositoryId
				FROM CommitScan
				WHERE CommitSha IS NOT NULL
				ORDER BY CompletedAtUtc DESC, CommitScanId DESC
				LIMIT 1
			), Counts AS (
				SELECT
					CommitScan.RepositoryId,
					CommitScan.ScanDefinitionId,
					CommitScan.CommitScanId,
					CommitScan.CommitSha,
					GitCommit.CommittedAtUtc,
					COALESCE(SUM(Measurement.Count), 0) AS Count
				FROM CommitScan
				JOIN ProjectScan ON ProjectScan.CommitScanId = CommitScan.CommitScanId
				JOIN LatestHistory ON LatestHistory.ScanDefinitionId = CommitScan.ScanDefinitionId AND LatestHistory.RepositoryId = CommitScan.RepositoryId
				LEFT JOIN Measurement ON Measurement.ProjectScanId = ProjectScan.ProjectScanId
					AND Measurement.Dimension = @dimension
					AND Measurement.Bucket = @bucket
				JOIN GitCommit ON GitCommit.RepositoryId = CommitScan.RepositoryId AND GitCommit.Sha = CommitScan.CommitSha
				WHERE CommitScan.CommitSha IS NOT NULL
				GROUP BY CommitScan.RepositoryId, CommitScan.ScanDefinitionId, CommitScan.CommitScanId, CommitScan.CommitSha, GitCommit.CommittedAtUtc
			), Ordered AS (
				SELECT
					CommitSha,
					CommittedAtUtc,
					Count,
					LAG(Count, 1, 0) OVER (
						PARTITION BY RepositoryId, ScanDefinitionId
						ORDER BY CommittedAtUtc, CommitScanId) AS PreviousCount
				FROM Counts
			)
			SELECT CommitSha, CommittedAtUtc, PreviousCount, Count
			FROM Ordered
			WHERE PreviousCount <> Count
			ORDER BY CommittedAtUtc, CommitSha;
			""";
		await using var command = CreateCommand(connection, null, sql, ("@dimension", dimension.ToString()), ("@bucket", bucket));
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var changes = new List<StatisticsCommitChange>();
		while (await reader.ReadAsync(cancellationToken))
		{
			changes.Add(new StatisticsCommitChange(
				reader.GetString(0),
				DateTimeOffset.Parse(reader.GetString(1), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind),
				dimension,
				bucket,
				reader.GetInt64(2),
				reader.GetInt64(3)));
		}

		var result = changes;

		return result;
	}

	private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
	{
		var builder = new SqliteConnectionStringBuilder
		{
			DataSource = _databasePath,
			Mode = SqliteOpenMode.ReadWriteCreate,
			ForeignKeys = true,
			Pooling = false
		};
		var connection = new SqliteConnection(builder.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		return connection;
	}

	private static async Task<int> ReadCurrentSchemaVersionAsync(SqliteConnection connection, SqliteTransaction? transaction, CancellationToken cancellationToken)
	{
		await using var command = CreateCommand(connection, transaction, "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;");
		var scalar = await command.ExecuteScalarAsync(cancellationToken);
		var result = Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);

		return result;
	}

	private static async Task<long> GetOrCreateRepositoryAsync(SqliteConnection connection, SqliteTransaction transaction, StatisticsRepositoryIdentity repository, CancellationToken cancellationToken)
	{
		const string upsertSql = """
			INSERT INTO Repository (CanonicalPath, GitDirectoryPath)
			VALUES (@canonicalPath, @gitDirectoryPath)
			ON CONFLICT(CanonicalPath) DO UPDATE SET GitDirectoryPath = excluded.GitDirectoryPath;
			""";
		await ExecuteNonQueryAsync(connection, transaction, upsertSql, cancellationToken,
			("@canonicalPath", repository.CanonicalPath),
			("@gitDirectoryPath", repository.GitDirectoryPath));
		var result = await ReadRequiredInt64Async(connection, transaction, "SELECT RepositoryId FROM Repository WHERE CanonicalPath = @canonicalPath;", cancellationToken, ("@canonicalPath", repository.CanonicalPath));

		return result;
	}

	private static async Task<long?> FindRepositoryIdAsync(SqliteConnection connection, SqliteTransaction? transaction, string canonicalPath, CancellationToken cancellationToken)
	{
		var result = await ReadNullableInt64Async(connection, transaction, "SELECT RepositoryId FROM Repository WHERE CanonicalPath = @canonicalPath;", cancellationToken, ("@canonicalPath", canonicalPath));

		return result;
	}

	private static async Task UpsertGitCommitAsync(SqliteConnection connection, SqliteTransaction transaction, long repositoryId, StatisticsGitCommit commit, CancellationToken cancellationToken)
	{
		const string commitSql = """
			INSERT INTO GitCommit (RepositoryId, Sha, TreeSha, AuthoredAtUtc, CommittedAtUtc, Subject, AuthorName)
			VALUES (@repositoryId, @sha, @treeSha, @authoredAtUtc, @committedAtUtc, @subject, @authorName)
			ON CONFLICT(RepositoryId, Sha) DO UPDATE SET
				TreeSha = excluded.TreeSha,
				AuthoredAtUtc = excluded.AuthoredAtUtc,
				CommittedAtUtc = excluded.CommittedAtUtc,
				Subject = excluded.Subject,
				AuthorName = excluded.AuthorName;
			""";
		await ExecuteNonQueryAsync(connection, transaction, commitSql, cancellationToken,
			("@repositoryId", repositoryId),
			("@sha", commit.Sha),
			("@treeSha", commit.TreeSha),
			("@authoredAtUtc", ToDatabaseTimestamp(commit.AuthoredAtUtc)),
			("@committedAtUtc", ToDatabaseTimestamp(commit.CommittedAtUtc)),
			("@subject", commit.Subject),
			("@authorName", commit.AuthorName));
		await ExecuteNonQueryAsync(connection, transaction, "DELETE FROM GitCommitParent WHERE RepositoryId = @repositoryId AND ChildSha = @childSha;", cancellationToken,
			("@repositoryId", repositoryId),
			("@childSha", commit.Sha));
		for (var index = 0; index < commit.ParentShas.Count; index++)
		{
			await ExecuteNonQueryAsync(connection, transaction, "INSERT INTO GitCommitParent (RepositoryId, ChildSha, ParentSha, ParentIndex) VALUES (@repositoryId, @childSha, @parentSha, @parentIndex);", cancellationToken,
				("@repositoryId", repositoryId),
				("@childSha", commit.Sha),
				("@parentSha", commit.ParentShas[index]),
				("@parentIndex", index));
		}
	}

	private static async Task<long> GetOrCreateScanDefinitionAsync(SqliteConnection connection, SqliteTransaction transaction, StatisticsScanDefinition definition, CancellationToken cancellationToken)
	{
		var optionsHash = definition.GetOptionsHash();
		const string sql = """
			INSERT INTO ScanDefinition (OptionsHash, Configuration, TargetFramework, GeneratedCodeMode, CollectorVersion)
			VALUES (@optionsHash, @configuration, @targetFramework, @generatedCodeMode, @collectorVersion)
			ON CONFLICT(OptionsHash) DO NOTHING;
			""";
		await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken,
			("@optionsHash", optionsHash),
			("@configuration", definition.Configuration),
			("@targetFramework", definition.TargetFramework),
			("@generatedCodeMode", definition.IncludeGeneratedCode ? "IncludeAll" : "Exclude"),
			("@collectorVersion", definition.CollectorVersion));
		var result = await ReadRequiredInt64Async(connection, transaction, "SELECT ScanDefinitionId FROM ScanDefinition WHERE OptionsHash = @optionsHash;", cancellationToken, ("@optionsHash", optionsHash));

		return result;
	}

	private static async Task<long?> FindScanDefinitionIdAsync(SqliteConnection connection, SqliteTransaction? transaction, string optionsHash, CancellationToken cancellationToken)
	{
		var result = await ReadNullableInt64Async(connection, transaction, "SELECT ScanDefinitionId FROM ScanDefinition WHERE OptionsHash = @optionsHash;", cancellationToken, ("@optionsHash", optionsHash));

		return result;
	}

	private static async Task DeleteExistingScanAsync(SqliteConnection connection, SqliteTransaction transaction, long definitionId, long? repositoryId, string? commitSha, string inputPath, CancellationToken cancellationToken)
	{
		const string sql = """
			DELETE FROM CommitScan
			WHERE ScanDefinitionId = @definitionId
			  AND RepositoryId IS @repositoryId
			  AND CommitSha IS @commitSha
			  AND InputPath = @inputPath;
			""";
		await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken,
			("@definitionId", definitionId),
			("@repositoryId", repositoryId),
			("@commitSha", commitSha),
			("@inputPath", inputPath));
	}

	private static async Task<long> InsertCommitScanAsync(SqliteConnection connection, SqliteTransaction transaction, long definitionId, long? repositoryId, string? commitSha, StatisticsScanSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string sql = """
			INSERT INTO CommitScan (ScanDefinitionId, RepositoryId, CommitSha, InputPath, Status, StartedAtUtc, CompletedAtUtc)
			VALUES (@definitionId, @repositoryId, @commitSha, @inputPath, @status, @startedAtUtc, @completedAtUtc);
			""";
		var now = ToDatabaseTimestamp(DateTimeOffset.UtcNow);
		await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken,
			("@definitionId", definitionId),
			("@repositoryId", repositoryId),
			("@commitSha", commitSha),
			("@inputPath", snapshot.InputPath),
			("@status", snapshot.Status.ToString()),
			("@startedAtUtc", now),
			("@completedAtUtc", now));
		var result = await ReadRequiredInt64Async(connection, transaction, "SELECT last_insert_rowid();", cancellationToken);

		return result;
	}

	private static async Task<long> InsertProjectScanAsync(SqliteConnection connection, SqliteTransaction transaction, long scanId, StatisticsProjectSnapshot project, CancellationToken cancellationToken)
	{
		const string sql = """
			INSERT INTO ProjectScan (CommitScanId, RelativeProjectPath, AssemblyName, TargetFramework, Status, SourceFileCount, TypeCount, MemberCount, CompilerErrorCount, UnresolvedObservationCount)
			VALUES (@scanId, @projectPath, @assemblyName, @targetFramework, @status, @sourceFileCount, @typeCount, @memberCount, @compilerErrorCount, @unresolvedObservationCount);
			""";
		var status = project.CompilerErrorCount == 0 ? StatisticsScanStatus.Complete : StatisticsScanStatus.Partial;
		await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken,
			("@scanId", scanId),
			("@projectPath", project.Identity.ProjectPath),
			("@assemblyName", project.Identity.AssemblyName),
			("@targetFramework", project.Identity.TargetFramework),
			("@status", status.ToString()),
			("@sourceFileCount", project.SourceFileCount),
			("@typeCount", project.TypeCount),
			("@memberCount", project.MemberCount),
			("@compilerErrorCount", project.CompilerErrorCount),
			("@unresolvedObservationCount", project.UnresolvedObservationCount));
		var projectId = await ReadRequiredInt64Async(connection, transaction, "SELECT last_insert_rowid();", cancellationToken);
		foreach (var measurement in project.Measurements)
		{
			await ExecuteNonQueryAsync(connection, transaction, "INSERT INTO Measurement (ProjectScanId, Dimension, Bucket, Count) VALUES (@projectId, @dimension, @bucket, @count);", cancellationToken,
				("@projectId", projectId),
				("@dimension", measurement.Dimension.ToString()),
				("@bucket", measurement.Bucket),
				("@count", measurement.Count));
		}

		foreach (var measurement in project.GroupedMeasurements)
		{
			await ExecuteNonQueryAsync(connection, transaction, "INSERT INTO GroupedMeasurement (ProjectScanId, Dimension, Bucket, GroupDimension, GroupBucket, Count) VALUES (@projectId, @dimension, @bucket, @groupDimension, @groupBucket, @count);", cancellationToken,
				("@projectId", projectId),
				("@dimension", measurement.Dimension.ToString()),
				("@bucket", measurement.Bucket),
				("@groupDimension", measurement.GroupDimension.ToString()),
				("@groupBucket", measurement.GroupBucket),
				("@count", measurement.Count));
		}

		return projectId;
	}

	private static async Task InsertFailureAsync(SqliteConnection connection, SqliteTransaction transaction, long scanId, long? projectId, StatisticsScanFailure failure, CancellationToken cancellationToken)
	{
		await ExecuteNonQueryAsync(connection, transaction, "INSERT INTO ScanFailure (CommitScanId, ProjectScanId, Stage, Message) VALUES (@scanId, @projectId, @stage, @message);", cancellationToken,
			("@scanId", scanId),
			("@projectId", projectId),
			("@stage", failure.Stage),
			("@message", failure.Message));
	}

	private static async Task<StatisticsStoredScan?> FindStoredScanAsync(SqliteConnection connection, SqliteTransaction? transaction, long definitionId, long? repositoryId, string? commitSha, string inputPath, CancellationToken cancellationToken)
	{
		const string sql = """
			SELECT CommitScanId, ScanDefinitionId, RepositoryId, CommitSha, InputPath, Status
			FROM CommitScan
			WHERE ScanDefinitionId = @definitionId
			  AND RepositoryId IS @repositoryId
			  AND CommitSha IS @commitSha
			  AND InputPath = @inputPath;
			""";
		await using var command = CreateCommand(connection, transaction, sql,
			("@definitionId", definitionId),
			("@repositoryId", repositoryId),
			("@commitSha", commitSha),
			("@inputPath", inputPath));
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return null;
		}

		var result = ReadStoredScan(reader);

		return result;
	}

	private static async Task<StatisticsSummary> ReadSummaryAsync(SqliteConnection connection, StatisticsStoredScan scan, CancellationToken cancellationToken)
	{
		const string measurementSql = """
			SELECT Measurement.Dimension, Measurement.Bucket, SUM(Measurement.Count)
			FROM ProjectScan
			JOIN Measurement ON Measurement.ProjectScanId = ProjectScan.ProjectScanId
			WHERE ProjectScan.CommitScanId = @scanId
			GROUP BY Measurement.Dimension, Measurement.Bucket
			ORDER BY Measurement.Dimension, Measurement.Bucket;
			""";
		var measurements = new List<StatisticsMeasurement>();
		await using (var measurementCommand = CreateCommand(connection, null, measurementSql, ("@scanId", scan.ScanId)))
		await using (var reader = await measurementCommand.ExecuteReaderAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				if (Enum.TryParse<StatisticsDimension>(reader.GetString(0), out var dimension))
				{
					measurements.Add(new StatisticsMeasurement(dimension, reader.GetString(1), reader.GetInt64(2)));
				}
			}
		}

		const string groupedMeasurementSql = """
			SELECT GroupedMeasurement.Dimension, GroupedMeasurement.Bucket, GroupedMeasurement.GroupDimension, GroupedMeasurement.GroupBucket, SUM(GroupedMeasurement.Count)
			FROM ProjectScan
			JOIN GroupedMeasurement ON GroupedMeasurement.ProjectScanId = ProjectScan.ProjectScanId
			WHERE ProjectScan.CommitScanId = @scanId
			GROUP BY GroupedMeasurement.Dimension, GroupedMeasurement.Bucket, GroupedMeasurement.GroupDimension, GroupedMeasurement.GroupBucket
			ORDER BY GroupedMeasurement.Dimension, GroupedMeasurement.GroupDimension, GroupedMeasurement.Bucket, GroupedMeasurement.GroupBucket;
			""";
		var groupedMeasurements = new List<StatisticsGroupedMeasurement>();
		await using (var measurementCommand = CreateCommand(connection, null, groupedMeasurementSql, ("@scanId", scan.ScanId)))
		await using (var reader = await measurementCommand.ExecuteReaderAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				if (Enum.TryParse<StatisticsDimension>(reader.GetString(0), out var dimension)
				    && Enum.TryParse<StatisticsDimension>(reader.GetString(2), out var groupDimension))
				{
					groupedMeasurements.Add(new StatisticsGroupedMeasurement(dimension, reader.GetString(1), groupDimension, reader.GetString(3), reader.GetInt64(4)));
				}
			}
		}

		var projectCount = await ReadRequiredInt64Async(connection, null, "SELECT COUNT(*) FROM ProjectScan WHERE CommitScanId = @scanId;", cancellationToken, ("@scanId", scan.ScanId));
		var failureCount = await ReadRequiredInt64Async(connection, null, "SELECT COUNT(*) FROM ScanFailure WHERE CommitScanId = @scanId;", cancellationToken, ("@scanId", scan.ScanId));
		var result = new StatisticsSummary(scan, measurements, checked((int)projectCount), checked((int)failureCount), groupedMeasurements);

		return result;
	}

	private static async Task RecordSchemaVersionAsync(SqliteConnection connection, SqliteTransaction transaction, int version, CancellationToken cancellationToken)
	{
		await ExecuteNonQueryAsync(connection, transaction, "INSERT INTO SchemaVersion (Version, AppliedAtUtc) VALUES (@version, @appliedAtUtc);", cancellationToken,
			("@version", version),
			("@appliedAtUtc", ToDatabaseTimestamp(DateTimeOffset.UtcNow)));
	}

	private static StatisticsStoredScan ReadStoredScan(SqliteDataReader reader)
	{
		var status = Enum.TryParse<StatisticsScanStatus>(reader.GetString(5), out var parsedStatus) ? parsedStatus : StatisticsScanStatus.Failed;
		var result = new StatisticsStoredScan(
			reader.GetInt64(0),
			reader.GetInt64(1),
			reader.IsDBNull(2) ? null : reader.GetInt64(2),
			reader.IsDBNull(3) ? null : reader.GetString(3),
			reader.GetString(4),
			status);

		return result;
	}

	private static async Task ExecuteNonQueryAsync(SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
	{
		await using var command = CreateCommand(connection, transaction, sql, parameters);
		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task<long> ReadRequiredInt64Async(SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
	{
		var value = await ReadNullableInt64Async(connection, transaction, sql, cancellationToken, parameters);
		if (value is null)
		{
			throw new InvalidOperationException("The database did not return a required numeric value.");
		}

		var result = value.Value;

		return result;
	}

	private static async Task<long?> ReadNullableInt64Async(SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
	{
		await using var command = CreateCommand(connection, transaction, sql, parameters);
		var value = await command.ExecuteScalarAsync(cancellationToken);
		if (value is null || value is DBNull)
		{
			return null;
		}

		var result = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);

		return result;
	}

	private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
	{
		var command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = sql;
		foreach (var parameter in parameters)
		{
			command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
		}

		return command;
	}

	private static string NormalizeProjectPath(string inputPath, string projectPath)
	{
		if (Path.IsPathRooted(projectPath))
		{
			var rootDirectory = Directory.Exists(inputPath)
				? inputPath
				: Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
			projectPath = Path.GetRelativePath(rootDirectory, projectPath);
		}

		var result = projectPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

		return result;
	}

	private static DateTimeOffset? ReadNullableTimestamp(SqliteDataReader reader, int ordinal)
	{
		if (reader.IsDBNull(ordinal))
		{
			return null;
		}

		var result = DateTimeOffset.Parse(reader.GetString(ordinal), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);

		return result;
	}

	private static string ToDatabaseTimestamp(DateTimeOffset timestamp)
	{
		var result = timestamp.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);

		return result;
	}
}
