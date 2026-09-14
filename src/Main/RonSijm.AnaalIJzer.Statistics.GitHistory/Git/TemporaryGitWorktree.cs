namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

public sealed class TemporaryGitWorktree : IAsyncDisposable
{
	private readonly GitCommandRunner _commandRunner;
	private readonly GitRepositoryInfo _repository;
	private bool _disposed;

	private TemporaryGitWorktree(GitCommandRunner commandRunner, GitRepositoryInfo repository, string path)
	{
		_commandRunner = commandRunner;
		_repository = repository;
		Path = path;
	}

	public string Path { get; }

	public static async Task<TemporaryGitWorktree> CreateAsync(GitCommandRunner commandRunner, GitRepositoryInfo repository, string initialCommitSha, CancellationToken cancellationToken)
	{
		var parentDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Anaaltomy", "worktrees");
		Directory.CreateDirectory(parentDirectory);
		var worktreePath = System.IO.Path.Combine(parentDirectory, Guid.NewGuid().ToString("N"));
		await commandRunner.RunRequiredAsync(repository.RootPath, ["worktree", "add", "--detach", worktreePath, initialCommitSha], cancellationToken);
		var result = new TemporaryGitWorktree(commandRunner, repository, worktreePath);

		return result;
	}

	public async Task CheckoutAsync(string commitSha, CancellationToken cancellationToken)
	{
		await _commandRunner.RunRequiredAsync(Path, ["checkout", "--detach", "--force", commitSha], cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		try
		{
			await _commandRunner.RunRequiredAsync(_repository.RootPath, ["worktree", "remove", "--force", Path], CancellationToken.None);
		}
		catch
		{
			if (Directory.Exists(Path))
			{
				Directory.Delete(Path, true);
			}
		}
	}
}
