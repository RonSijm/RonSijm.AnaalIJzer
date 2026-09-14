using RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Tests.Git;

internal sealed class GitTestRepository : IAsyncDisposable
{
	private readonly GitCommandRunner _runner = new();

	private GitTestRepository(string rootPath)
	{
		RootPath = rootPath;
	}

	public string RootPath { get; }

	public static async Task<GitTestRepository> CreateWithMergeAsync(CancellationToken cancellationToken)
	{
		var rootPath = Path.Combine(Path.GetTempPath(), "Anaaltomy", "GitTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(rootPath);
		var repository = new GitTestRepository(rootPath);
		await repository._runner.RunRequiredAsync(rootPath, ["init"], cancellationToken);
		await repository._runner.RunRequiredAsync(rootPath, ["config", "user.email", "anaaltomy@example.test"], cancellationToken);
		await repository._runner.RunRequiredAsync(rootPath, ["config", "user.name", "Anaaltomy Tests"], cancellationToken);
		File.WriteAllText(Path.Combine(rootPath, "Pizza.csproj"), """
			<Project Sdk="Microsoft.NET.Sdk">
			  <PropertyGroup>
			    <TargetFramework>net10.0</TargetFramework>
			    <Nullable>enable</Nullable>
			  </PropertyGroup>
			</Project>
			""");
		File.WriteAllText(Path.Combine(rootPath, "Pizza.cs"), "public class Pizza { }");
		await repository.CommitAsync("Root", cancellationToken);
		var primaryBranch = await repository._runner.RunRequiredAsync(rootPath, ["branch", "--show-current"], cancellationToken);
		await repository._runner.RunRequiredAsync(rootPath, ["checkout", "-b", "feature"], cancellationToken);
		File.WriteAllText(Path.Combine(rootPath, "Feature.cs"), "public class Feature { }");
		await repository.CommitAsync("Feature", cancellationToken);
		await repository._runner.RunRequiredAsync(rootPath, ["checkout", primaryBranch], cancellationToken);
		File.WriteAllText(Path.Combine(rootPath, "Main.cs"), "public class Main { }");
		await repository.CommitAsync("Main", cancellationToken);
		await repository._runner.RunRequiredAsync(rootPath, ["merge", "--no-ff", "feature", "-m", "Merge feature"], cancellationToken);

		return repository;
	}

	public ValueTask DisposeAsync()
	{
		if (Directory.Exists(RootPath))
		{
			foreach (var item in Directory.EnumerateFileSystemEntries(RootPath, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(item, File.GetAttributes(item) & ~FileAttributes.ReadOnly);
			}

			Directory.Delete(RootPath, true);
		}

		return ValueTask.CompletedTask;
	}

	private async Task CommitAsync(string message, CancellationToken cancellationToken)
	{
		await _runner.RunRequiredAsync(RootPath, ["add", "."], cancellationToken);
		await _runner.RunRequiredAsync(RootPath, ["commit", "-m", message], cancellationToken);
	}
}
