using RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

public sealed class GitRepositoryReader(GitCommandRunner commandRunner)
{
	public async Task<GitRepositoryInfo> ResolveAsync(string repositoryPath, CancellationToken cancellationToken)
	{
		var fullRepositoryPath = Path.GetFullPath(repositoryPath);
		if (!Directory.Exists(fullRepositoryPath))
		{
			throw new DirectoryNotFoundException("Repository directory not found: " + fullRepositoryPath);
		}

		var rootPath = await commandRunner.RunRequiredAsync(fullRepositoryPath, ["rev-parse", "--show-toplevel"], cancellationToken);
		var gitDirectoryPath = await commandRunner.RunRequiredAsync(fullRepositoryPath, ["rev-parse", "--absolute-git-dir"], cancellationToken);
		var result = new GitRepositoryInfo(Path.GetFullPath(rootPath), Path.GetFullPath(gitDirectoryPath));

		return result;
	}

	public async Task<IReadOnlyList<StatisticsGitCommit>> ReadCommitsAsync(GitRepositoryInfo repository, GitHistorySelection selection, CancellationToken cancellationToken)
	{
		selection.Validate();
		var arguments = new List<string> { "rev-list", "--topo-order", "--reverse", "--parents" };
		if (selection.FirstParent)
		{
			arguments.Add("--first-parent");
		}

		arguments.Add(GetRevisionSelection(selection));
		var output = await commandRunner.RunRequiredAsync(repository.RootPath, arguments, cancellationToken);
		var commits = new List<StatisticsGitCommit>();
		foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				continue;
			}

			commits.Add(await ReadCommitAsync(repository.RootPath, parts[0], parts.Skip(1).ToArray(), selection.IncludeCommitMetadata, cancellationToken));
		}

		IReadOnlyList<StatisticsGitCommit> result = selection.MaximumCommitCount is null ? commits : commits.Take(selection.MaximumCommitCount.Value).ToArray();

		return result;
	}

	private async Task<StatisticsGitCommit> ReadCommitAsync(string repositoryPath, string sha, IReadOnlyList<string> parentShas, bool includeMetadata, CancellationToken cancellationToken)
	{
		var format = includeMetadata ? "%T%x1f%aI%x1f%cI%x1f%s%x1f%an" : "%T%x1f%aI%x1f%cI";
		var output = await commandRunner.RunRequiredAsync(repositoryPath, ["show", "-s", "--format=" + format, sha], cancellationToken);
		var parts = output.Split('\u001f');
		if (parts.Length < 3)
		{
			throw new InvalidOperationException("Git did not return complete metadata for commit " + sha + ".");
		}

		var result = new StatisticsGitCommit(
			sha,
			parts[0],
			DateTimeOffset.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind),
			DateTimeOffset.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind),
			parentShas,
			includeMetadata && parts.Length > 3 ? parts[3] : null,
			includeMetadata && parts.Length > 4 ? parts[4] : null);

		return result;
	}

	private static string GetRevisionSelection(GitHistorySelection selection)
	{
		if (selection.FromRoot)
		{
			return selection.ToRevision ?? "HEAD";
		}

		if (!string.IsNullOrWhiteSpace(selection.FromRevision))
		{
			var result = selection.FromRevision + ".." + (selection.ToRevision ?? "HEAD");

			return result;
		}

		return selection.ToRevision!;
	}
}
