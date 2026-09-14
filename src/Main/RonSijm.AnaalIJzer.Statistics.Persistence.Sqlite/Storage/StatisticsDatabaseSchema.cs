namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Storage;

internal static class StatisticsDatabaseSchema
{
	public const int LatestVersion = 2;

	public static readonly string[] VersionOneStatements =
	[
		"""
		CREATE TABLE IF NOT EXISTS SchemaVersion (
			Version INTEGER NOT NULL PRIMARY KEY,
			AppliedAtUtc TEXT NOT NULL
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS Repository (
			RepositoryId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
			CanonicalPath TEXT NOT NULL UNIQUE,
			GitDirectoryPath TEXT NULL
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS GitCommit (
			RepositoryId INTEGER NOT NULL,
			Sha TEXT NOT NULL,
			TreeSha TEXT NOT NULL,
			AuthoredAtUtc TEXT NOT NULL,
			CommittedAtUtc TEXT NOT NULL,
			Subject TEXT NULL,
			AuthorName TEXT NULL,
			PRIMARY KEY (RepositoryId, Sha),
			FOREIGN KEY (RepositoryId) REFERENCES Repository(RepositoryId) ON DELETE CASCADE
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS GitCommitParent (
			RepositoryId INTEGER NOT NULL,
			ChildSha TEXT NOT NULL,
			ParentSha TEXT NOT NULL,
			ParentIndex INTEGER NOT NULL,
			PRIMARY KEY (RepositoryId, ChildSha, ParentIndex),
			FOREIGN KEY (RepositoryId, ChildSha) REFERENCES GitCommit(RepositoryId, Sha) ON DELETE CASCADE
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS ScanDefinition (
			ScanDefinitionId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
			OptionsHash TEXT NOT NULL UNIQUE,
			Configuration TEXT NOT NULL,
			TargetFramework TEXT NULL,
			GeneratedCodeMode TEXT NOT NULL,
			CollectorVersion TEXT NOT NULL
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS CommitScan (
			CommitScanId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
			ScanDefinitionId INTEGER NOT NULL,
			RepositoryId INTEGER NULL,
			CommitSha TEXT NULL,
			InputPath TEXT NOT NULL,
			Status TEXT NOT NULL,
			StartedAtUtc TEXT NOT NULL,
			CompletedAtUtc TEXT NULL,
			FOREIGN KEY (ScanDefinitionId) REFERENCES ScanDefinition(ScanDefinitionId) ON DELETE CASCADE,
			FOREIGN KEY (RepositoryId) REFERENCES Repository(RepositoryId) ON DELETE CASCADE
		);
		""",
		"CREATE UNIQUE INDEX IF NOT EXISTS IX_CommitScan_Identity ON CommitScan(ScanDefinitionId, IFNULL(RepositoryId, -1), IFNULL(CommitSha, ''), InputPath);",
		"""
		CREATE TABLE IF NOT EXISTS ProjectScan (
			ProjectScanId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
			CommitScanId INTEGER NOT NULL,
			RelativeProjectPath TEXT NOT NULL,
			AssemblyName TEXT NULL,
			TargetFramework TEXT NULL,
			Status TEXT NOT NULL,
			SourceFileCount INTEGER NOT NULL,
			TypeCount INTEGER NOT NULL,
			MemberCount INTEGER NOT NULL,
			CompilerErrorCount INTEGER NOT NULL,
			UnresolvedObservationCount INTEGER NOT NULL,
			FOREIGN KEY (CommitScanId) REFERENCES CommitScan(CommitScanId) ON DELETE CASCADE,
			UNIQUE (CommitScanId, RelativeProjectPath, TargetFramework)
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS Measurement (
			ProjectScanId INTEGER NOT NULL,
			Dimension TEXT NOT NULL,
			Bucket TEXT NOT NULL,
			Count INTEGER NOT NULL,
			PRIMARY KEY (ProjectScanId, Dimension, Bucket),
			FOREIGN KEY (ProjectScanId) REFERENCES ProjectScan(ProjectScanId) ON DELETE CASCADE
		);
		""",
		"""
		CREATE TABLE IF NOT EXISTS ScanFailure (
			ScanFailureId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
			CommitScanId INTEGER NOT NULL,
			ProjectScanId INTEGER NULL,
			Stage TEXT NOT NULL,
			Message TEXT NOT NULL,
			FOREIGN KEY (CommitScanId) REFERENCES CommitScan(CommitScanId) ON DELETE CASCADE,
			FOREIGN KEY (ProjectScanId) REFERENCES ProjectScan(ProjectScanId) ON DELETE CASCADE
		);
		""",
		"CREATE INDEX IF NOT EXISTS IX_ProjectScan_CommitScanId ON ProjectScan(CommitScanId);",
		"CREATE INDEX IF NOT EXISTS IX_Measurement_DimensionBucket ON Measurement(Dimension, Bucket);",
		"CREATE INDEX IF NOT EXISTS IX_GitCommit_RepositoryCommittedAt ON GitCommit(RepositoryId, CommittedAtUtc);"
	];

	public static readonly string[] VersionTwoStatements =
	[
		"""
		CREATE TABLE IF NOT EXISTS GroupedMeasurement (
			ProjectScanId INTEGER NOT NULL,
			Dimension TEXT NOT NULL,
			Bucket TEXT NOT NULL,
			GroupDimension TEXT NOT NULL,
			GroupBucket TEXT NOT NULL,
			Count INTEGER NOT NULL,
			PRIMARY KEY (ProjectScanId, Dimension, Bucket, GroupDimension, GroupBucket),
			FOREIGN KEY (ProjectScanId) REFERENCES ProjectScan(ProjectScanId) ON DELETE CASCADE
		);
		""",
		"CREATE INDEX IF NOT EXISTS IX_GroupedMeasurement_Dimensions ON GroupedMeasurement(Dimension, GroupDimension, Bucket, GroupBucket);"
	];
}
