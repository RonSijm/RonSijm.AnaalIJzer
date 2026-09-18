using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace RonSijm.Anaaltomy.CommandLine;

internal static class AnaaltomyDatabaseExporter
{
	private static readonly string[] ExportTableNames =
	[
		nameof(SchemaVersion),
		nameof(Repository),
		nameof(GitCommit),
		nameof(GitCommitParent),
		nameof(ScanDefinition),
		nameof(CommitScan),
		nameof(ProjectScan),
		nameof(Measurement),
		nameof(GroupedMeasurement),
		nameof(ScanFailure)
	];

	public static async Task<IReadOnlyList<string>> ExportAsync(string databasePath, string format, string outputDirectory, CancellationToken cancellationToken)
	{
		var fullDatabasePath = Path.GetFullPath(databasePath);
		if (!File.Exists(fullDatabasePath))
		{
			throw new ArgumentException("The database file does not exist: " + fullDatabasePath);
		}

		var fullOutputDirectory = Path.GetFullPath(outputDirectory);
		Directory.CreateDirectory(fullOutputDirectory);
		var normalizedFormat = NormalizeFormat(format);
		CleanKnownOutputFiles(fullOutputDirectory, normalizedFormat.Extension);
		var export = await ReadDatabaseAsync(fullDatabasePath, cancellationToken);
		await WriteExportAsync(export, normalizedFormat.Provider, fullOutputDirectory, cancellationToken);
		var result = ExportTableNames
			.Select(tableName => Path.Combine(fullOutputDirectory, tableName + normalizedFormat.Extension))
			.Where(File.Exists)
			.ToArray();

		return result;
	}

	private static async Task<AnaaltomyDatabaseExport> ReadDatabaseAsync(string databasePath, CancellationToken cancellationToken)
	{
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = databasePath,
			Pooling = false
		}.ToString();
		await using var connection = new SqliteConnection(connectionString);
		try
		{
			await connection.OpenAsync(cancellationToken);
			var result = new AnaaltomyDatabaseExport(
				await ReadRowsAsync(connection, "SELECT Version, AppliedAtUtc FROM SchemaVersion ORDER BY Version;", reader => new SchemaVersion
			{
				Version = reader.GetInt32(0),
				AppliedAtUtc = reader.GetString(1)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT RepositoryId, CanonicalPath, GitDirectoryPath FROM Repository ORDER BY RepositoryId;", reader => new Repository
			{
				RepositoryId = reader.GetInt64(0),
				CanonicalPath = reader.GetString(1),
				GitDirectoryPath = GetNullableString(reader, 2)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT RepositoryId, Sha, TreeSha, AuthoredAtUtc, CommittedAtUtc, Subject, AuthorName FROM GitCommit ORDER BY RepositoryId, Sha;", reader => new GitCommit
			{
				RepositoryId = reader.GetInt64(0),
				Sha = reader.GetString(1),
				TreeSha = reader.GetString(2),
				AuthoredAtUtc = reader.GetString(3),
				CommittedAtUtc = reader.GetString(4),
				Subject = GetNullableString(reader, 5),
				AuthorName = GetNullableString(reader, 6)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT RepositoryId, ChildSha, ParentSha, ParentIndex FROM GitCommitParent ORDER BY RepositoryId, ChildSha, ParentIndex;", reader => new GitCommitParent
			{
				RepositoryId = reader.GetInt64(0),
				ChildSha = reader.GetString(1),
				ParentSha = reader.GetString(2),
				ParentIndex = reader.GetInt32(3)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT ScanDefinitionId, OptionsHash, Configuration, TargetFramework, GeneratedCodeMode, CollectorVersion FROM ScanDefinition ORDER BY ScanDefinitionId;", reader => new ScanDefinition
			{
				ScanDefinitionId = reader.GetInt64(0),
				OptionsHash = reader.GetString(1),
				Configuration = reader.GetString(2),
				TargetFramework = GetNullableString(reader, 3),
				GeneratedCodeMode = reader.GetString(4),
				CollectorVersion = reader.GetString(5)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT CommitScanId, ScanDefinitionId, RepositoryId, CommitSha, InputPath, Status, StartedAtUtc, CompletedAtUtc FROM CommitScan ORDER BY CommitScanId;", reader => new CommitScan
			{
				CommitScanId = reader.GetInt64(0),
				ScanDefinitionId = reader.GetInt64(1),
				RepositoryId = GetNullableInt64(reader, 2),
				CommitSha = GetNullableString(reader, 3),
				InputPath = reader.GetString(4),
				Status = reader.GetString(5),
				StartedAtUtc = reader.GetString(6),
				CompletedAtUtc = GetNullableString(reader, 7)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT ProjectScanId, CommitScanId, RelativeProjectPath, AssemblyName, TargetFramework, Status, SourceFileCount, TypeCount, MemberCount, CompilerErrorCount, UnresolvedObservationCount FROM ProjectScan ORDER BY ProjectScanId;", reader => new ProjectScan
			{
				ProjectScanId = reader.GetInt64(0),
				CommitScanId = reader.GetInt64(1),
				RelativeProjectPath = reader.GetString(2),
				AssemblyName = GetNullableString(reader, 3),
				TargetFramework = GetNullableString(reader, 4),
				Status = reader.GetString(5),
				SourceFileCount = reader.GetInt32(6),
				TypeCount = reader.GetInt32(7),
				MemberCount = reader.GetInt32(8),
				CompilerErrorCount = reader.GetInt32(9),
				UnresolvedObservationCount = reader.GetInt32(10)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT ProjectScanId, Dimension, Bucket, Count FROM Measurement ORDER BY ProjectScanId, Dimension, Bucket;", reader => new Measurement
			{
				ProjectScanId = reader.GetInt64(0),
				Dimension = reader.GetString(1),
				Bucket = reader.GetString(2),
				Count = reader.GetInt64(3)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT ProjectScanId, Dimension, Bucket, GroupDimension, GroupBucket, Count FROM GroupedMeasurement ORDER BY ProjectScanId, Dimension, Bucket, GroupDimension, GroupBucket;", reader => new GroupedMeasurement
			{
				ProjectScanId = reader.GetInt64(0),
				Dimension = reader.GetString(1),
				Bucket = reader.GetString(2),
				GroupDimension = reader.GetString(3),
				GroupBucket = reader.GetString(4),
				Count = reader.GetInt64(5)
			}, cancellationToken),
				await ReadRowsAsync(connection, "SELECT ScanFailureId, CommitScanId, ProjectScanId, Stage, Message FROM ScanFailure ORDER BY ScanFailureId;", reader => new ScanFailure
			{
				ScanFailureId = reader.GetInt64(0),
				CommitScanId = reader.GetInt64(1),
				ProjectScanId = GetNullableInt64(reader, 2),
				Stage = reader.GetString(3),
				Message = reader.GetString(4)
			}, cancellationToken));

			return result;
		}
		finally
		{
			SqliteConnection.ClearPool(connection);
		}
	}

	private static async Task<List<T>> ReadRowsAsync<T>(SqliteConnection connection, string sql, Func<SqliteDataReader, T> map, CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var result = new List<T>();
		while (await reader.ReadAsync(cancellationToken))
		{
			result.Add(map(reader));
		}

		return result;
	}

	private static async Task WriteExportAsync(AnaaltomyDatabaseExport export, string provider, string outputDirectory, CancellationToken cancellationToken)
	{
		var optionsBuilder = new DbContextOptionsBuilder<AnaaltomyExportDbContext>();
		switch (provider)
		{
			case "csv":
				optionsBuilder.UseCsv(outputDirectory);
				break;
			case "json":
				optionsBuilder.UseJson(outputDirectory);
				break;
			case "markdown":
				optionsBuilder.UseMarkdown(outputDirectory);
				break;
			default:
				throw new ArgumentException("Unsupported database export format: " + provider + ". Use json, csv, or markdown.");
		}

		await using var context = new AnaaltomyExportDbContext(optionsBuilder.Options);
		await context.Database.EnsureCreatedAsync(cancellationToken);
		context.SchemaVersions.AddRange(export.SchemaVersions);
		context.Repositories.AddRange(export.Repositories);
		context.GitCommits.AddRange(export.GitCommits);
		context.GitCommitParents.AddRange(export.GitCommitParents);
		context.ScanDefinitions.AddRange(export.ScanDefinitions);
		context.CommitScans.AddRange(export.CommitScans);
		context.ProjectScans.AddRange(export.ProjectScans);
		context.Measurements.AddRange(export.Measurements);
		context.GroupedMeasurements.AddRange(export.GroupedMeasurements);
		context.ScanFailures.AddRange(export.ScanFailures);
		await context.SaveChangesAsync(cancellationToken);
	}

	private static (string Provider, string Extension) NormalizeFormat(string format)
	{
		var result = format.ToLowerInvariant() switch
		{
			"csv" => ("csv", ".csv"),
			"json" => ("json", ".json"),
			"markdown" or "md" => ("markdown", ".md"),
			_ => throw new ArgumentException("Unsupported database export format: " + format + ". Use json, csv, or markdown.")
		};

		return result;
	}

	private static void CleanKnownOutputFiles(string outputDirectory, string extension)
	{
		foreach (var tableName in ExportTableNames)
		{
			var outputPath = Path.Combine(outputDirectory, tableName + extension);
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}
		}
	}

	private static string? GetNullableString(SqliteDataReader reader, int ordinal)
	{
		var result = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

		return result;
	}

	private static long? GetNullableInt64(SqliteDataReader reader, int ordinal)
	{
		var result = reader.IsDBNull(ordinal) ? (long?)null : reader.GetInt64(ordinal);

		return result;
	}

	private sealed record AnaaltomyDatabaseExport(
		List<SchemaVersion> SchemaVersions,
		List<Repository> Repositories,
		List<GitCommit> GitCommits,
		List<GitCommitParent> GitCommitParents,
		List<ScanDefinition> ScanDefinitions,
		List<CommitScan> CommitScans,
		List<ProjectScan> ProjectScans,
		List<Measurement> Measurements,
		List<GroupedMeasurement> GroupedMeasurements,
		List<ScanFailure> ScanFailures);

	private sealed class AnaaltomyExportDbContext(DbContextOptions<AnaaltomyExportDbContext> options) : DbContext(options)
	{
		public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();
		public DbSet<Repository> Repositories => Set<Repository>();
		public DbSet<GitCommit> GitCommits => Set<GitCommit>();
		public DbSet<GitCommitParent> GitCommitParents => Set<GitCommitParent>();
		public DbSet<ScanDefinition> ScanDefinitions => Set<ScanDefinition>();
		public DbSet<CommitScan> CommitScans => Set<CommitScan>();
		public DbSet<ProjectScan> ProjectScans => Set<ProjectScan>();
		public DbSet<Measurement> Measurements => Set<Measurement>();
		public DbSet<GroupedMeasurement> GroupedMeasurements => Set<GroupedMeasurement>();
		public DbSet<ScanFailure> ScanFailures => Set<ScanFailure>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<SchemaVersion>(entity =>
			{
				entity.HasKey(item => item.Version);
				entity.Property(item => item.Version).ValueGeneratedNever();
			});
			modelBuilder.Entity<Repository>(entity =>
			{
				entity.HasKey(item => item.RepositoryId);
				entity.Property(item => item.RepositoryId).ValueGeneratedNever();
			});
			modelBuilder.Entity<GitCommit>().HasKey(item => new { item.RepositoryId, item.Sha });
			modelBuilder.Entity<GitCommitParent>().HasKey(item => new { item.RepositoryId, item.ChildSha, item.ParentIndex });
			modelBuilder.Entity<ScanDefinition>(entity =>
			{
				entity.HasKey(item => item.ScanDefinitionId);
				entity.Property(item => item.ScanDefinitionId).ValueGeneratedNever();
			});
			modelBuilder.Entity<CommitScan>(entity =>
			{
				entity.HasKey(item => item.CommitScanId);
				entity.Property(item => item.CommitScanId).ValueGeneratedNever();
			});
			modelBuilder.Entity<ProjectScan>(entity =>
			{
				entity.HasKey(item => item.ProjectScanId);
				entity.Property(item => item.ProjectScanId).ValueGeneratedNever();
			});
			modelBuilder.Entity<Measurement>().HasKey(item => new { item.ProjectScanId, item.Dimension, item.Bucket });
			modelBuilder.Entity<GroupedMeasurement>().HasKey(item => new { item.ProjectScanId, item.Dimension, item.Bucket, item.GroupDimension, item.GroupBucket });
			modelBuilder.Entity<ScanFailure>(entity =>
			{
				entity.HasKey(item => item.ScanFailureId);
				entity.Property(item => item.ScanFailureId).ValueGeneratedNever();
			});
		}
	}

	private sealed class SchemaVersion
	{
		public int Version { get; set; }
		public string AppliedAtUtc { get; set; } = "";
	}

	private sealed class Repository
	{
		public long RepositoryId { get; set; }
		public string CanonicalPath { get; set; } = "";
		public string? GitDirectoryPath { get; set; }
	}

	private sealed class GitCommit
	{
		public long RepositoryId { get; set; }
		public string Sha { get; set; } = "";
		public string TreeSha { get; set; } = "";
		public string AuthoredAtUtc { get; set; } = "";
		public string CommittedAtUtc { get; set; } = "";
		public string? Subject { get; set; }
		public string? AuthorName { get; set; }
	}

	private sealed class GitCommitParent
	{
		public long RepositoryId { get; set; }
		public string ChildSha { get; set; } = "";
		public string ParentSha { get; set; } = "";
		public int ParentIndex { get; set; }
	}

	private sealed class ScanDefinition
	{
		public long ScanDefinitionId { get; set; }
		public string OptionsHash { get; set; } = "";
		public string Configuration { get; set; } = "";
		public string? TargetFramework { get; set; }
		public string GeneratedCodeMode { get; set; } = "";
		public string CollectorVersion { get; set; } = "";
	}

	private sealed class CommitScan
	{
		public long CommitScanId { get; set; }
		public long ScanDefinitionId { get; set; }
		public long? RepositoryId { get; set; }
		public string? CommitSha { get; set; }
		public string InputPath { get; set; } = "";
		public string Status { get; set; } = "";
		public string StartedAtUtc { get; set; } = "";
		public string? CompletedAtUtc { get; set; }
	}

	private sealed class ProjectScan
	{
		public long ProjectScanId { get; set; }
		public long CommitScanId { get; set; }
		public string RelativeProjectPath { get; set; } = "";
		public string? AssemblyName { get; set; }
		public string? TargetFramework { get; set; }
		public string Status { get; set; } = "";
		public int SourceFileCount { get; set; }
		public int TypeCount { get; set; }
		public int MemberCount { get; set; }
		public int CompilerErrorCount { get; set; }
		public int UnresolvedObservationCount { get; set; }
	}

	private sealed class Measurement
	{
		public long ProjectScanId { get; set; }
		public string Dimension { get; set; } = "";
		public string Bucket { get; set; } = "";
		public long Count { get; set; }
	}

	private sealed class GroupedMeasurement
	{
		public long ProjectScanId { get; set; }
		public string Dimension { get; set; } = "";
		public string Bucket { get; set; } = "";
		public string GroupDimension { get; set; } = "";
		public string GroupBucket { get; set; } = "";
		public long Count { get; set; }
	}

	private sealed class ScanFailure
	{
		public long ScanFailureId { get; set; }
		public long CommitScanId { get; set; }
		public long? ProjectScanId { get; set; }
		public string Stage { get; set; } = "";
		public string Message { get; set; } = "";
	}
}
