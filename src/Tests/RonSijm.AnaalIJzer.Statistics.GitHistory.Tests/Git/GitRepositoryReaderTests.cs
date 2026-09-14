using RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Tests.Git;

public sealed class GitRepositoryReaderTests
{
	[Fact]
	public async Task ReadCommitsAsync_PreservesMergeParentsAndTemporaryWorktreeLeavesActiveCheckoutUntouched()
	{
		await using var repository = await GitTestRepository.CreateWithMergeAsync(TestContext.Current.CancellationToken);
		var runner = new GitCommandRunner();
		var reader = new GitRepositoryReader(runner);
		var info = await reader.ResolveAsync(repository.RootPath, TestContext.Current.CancellationToken);
		var activeHeadBefore = await runner.RunRequiredAsync(repository.RootPath, ["rev-parse", "HEAD"], TestContext.Current.CancellationToken);

		var commits = await reader.ReadCommitsAsync(info, new GitHistorySelection(true, includeCommitMetadata: true), TestContext.Current.CancellationToken);
		var mergeCommit = commits.Single(commit => commit.ParentShas.Count == 2);
		await using (var worktree = await TemporaryGitWorktree.CreateAsync(runner, info, mergeCommit.Sha, TestContext.Current.CancellationToken))
		{
			await worktree.CheckoutAsync(commits[0].Sha, TestContext.Current.CancellationToken);
			File.Exists(Path.Combine(worktree.Path, "Pizza.cs")).Should().BeTrue();
		}

		var activeHeadAfter = await runner.RunRequiredAsync(repository.RootPath, ["rev-parse", "HEAD"], TestContext.Current.CancellationToken);

		commits.Should().HaveCountGreaterThanOrEqualTo(4);
		mergeCommit.ParentShas.Should().HaveCount(2);
		mergeCommit.Subject.Should().Be("Merge feature");
		activeHeadAfter.Should().Be(activeHeadBefore);
	}

	[Fact]
	public async Task ReadCommitsAsync_FirstParentFollowsThePrimaryIntegrationPath()
	{
		await using var repository = await GitTestRepository.CreateWithMergeAsync(TestContext.Current.CancellationToken);
		var runner = new GitCommandRunner();
		var reader = new GitRepositoryReader(runner);
		var info = await reader.ResolveAsync(repository.RootPath, TestContext.Current.CancellationToken);

		var allCommits = await reader.ReadCommitsAsync(info, new GitHistorySelection(true), TestContext.Current.CancellationToken);
		var firstParentCommits = await reader.ReadCommitsAsync(info, new GitHistorySelection(true, firstParent: true), TestContext.Current.CancellationToken);

		firstParentCommits.Count.Should().BeLessThan(allCommits.Count);
	}

	[Fact]
	public void Validate_RejectsAmbiguousRootAndRangeSelection()
	{
		var selection = new GitHistorySelection(true, "v0.1.0");

		var action = selection.Validate;

		action.Should().Throw<ArgumentException>().WithMessage("*either --from-root or --from*");
	}
}
