using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.AnaalIJzer.Statistics.GitHistory.Git;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;
using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Storage;
using RonSijm.AnaalIJzer.Statistics.Workspace.Scanning;

namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Scanning;

public sealed class StatisticsGitHistoryScanner(
	GitRepositoryReader repositoryReader,
	GitCommandRunner commandRunner,
	StatisticsWorkspaceScanner workspaceScanner,
	StatisticsSqliteDatabase database)
{
	public async Task<StatisticsGitHistoryScanResult> ScanAsync(StatisticsGitHistoryScanRequest request, IProgress<string>? progress, CancellationToken cancellationToken)
	{
		var repository = await repositoryReader.ResolveAsync(request.RepositoryPath, cancellationToken);
		var repositoryIdentity = new StatisticsRepositoryIdentity(repository.RootPath, repository.GitDirectoryPath);
		var commits = await repositoryReader.ReadCommitsAsync(repository, request.Selection, cancellationToken);
		await database.PersistGitCommitsAsync(repositoryIdentity, commits, cancellationToken);
		var failures = new List<StatisticsScanFailure>();
		var scannedCommitCount = 0;
		var skippedCompleteCommitCount = 0;
		TemporaryGitWorktree? worktree = null;
		try
		{
			foreach (var commit in commits)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var existing = await database.FindCommitScanAsync(request.Definition, repositoryIdentity, commit.Sha, repository.RootPath, cancellationToken);
				if (request.Resume && existing?.Status == StatisticsScanStatus.Complete)
				{
					skippedCompleteCommitCount++;
					progress?.Report("Skipped complete commit " + commit.Sha + ".");
					continue;
				}

				try
				{
					if (worktree is null)
					{
						worktree = await TemporaryGitWorktree.CreateAsync(commandRunner, repository, commit.Sha, cancellationToken);
					}
					else
					{
						await worktree.CheckoutAsync(commit.Sha, cancellationToken);
					}

					progress?.Report("Scanning commit " + commit.Sha + ".");
					var scanRequest = new StatisticsWorkspaceScanRequest(
						StatisticsWorkspaceInputKind.Directory,
						worktree.Path,
						request.Definition.Configuration,
						request.Definition.TargetFramework,
						request.Definition.IncludeGeneratedCode,
						request.RestoreMode);
					var workspaceSnapshot = await workspaceScanner.ScanAsync(scanRequest, cancellationToken);
					var snapshot = NormalizeSnapshotForRepository(workspaceSnapshot, repository.RootPath, worktree.Path);
					await database.PersistScanAsync(new StatisticsPersistRequest(request.Definition, snapshot, repositoryIdentity, commit), cancellationToken);
					if (snapshot.Status != StatisticsScanStatus.Complete)
					{
						failures.AddRange(snapshot.Failures);
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception exception)
				{
					var failure = new StatisticsScanFailure(null, "History", "Commit " + commit.Sha + ": " + exception.Message);
					failures.Add(failure);
					var failedSnapshot = new StatisticsScanSnapshot(repository.RootPath, StatisticsScanStatus.Failed, [], [failure]);
					await database.PersistScanAsync(new StatisticsPersistRequest(request.Definition, failedSnapshot, repositoryIdentity, commit), cancellationToken);
				}

				scannedCommitCount++;
			}
		}
		finally
		{
			if (worktree is not null)
			{
				await worktree.DisposeAsync();
			}
		}

		var result = new StatisticsGitHistoryScanResult(commits.Count, scannedCommitCount, skippedCompleteCommitCount, failures);

		return result;
	}

	private static StatisticsScanSnapshot NormalizeSnapshotForRepository(StatisticsScanSnapshot snapshot, string repositoryPath, string worktreePath)
	{
		var failures = snapshot.Failures.Select(failure => new StatisticsScanFailure(
			NormalizeFailureProjectPath(failure.ProjectPath, worktreePath),
			failure.Stage,
			failure.Message)).ToArray();
		var result = new StatisticsScanSnapshot(repositoryPath, snapshot.Status, snapshot.Projects, failures);

		return result;
	}

	private static string? NormalizeFailureProjectPath(string? projectPath, string worktreePath)
	{
		if (string.IsNullOrWhiteSpace(projectPath) || !Path.IsPathRooted(projectPath))
		{
			return projectPath;
		}

		var result = Path.GetRelativePath(worktreePath, projectPath);

		return result;
	}
}
