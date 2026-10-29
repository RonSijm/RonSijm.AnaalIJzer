namespace RonSijm.IJzer.Statistics.GitHistory.Git;

public sealed class GitRepositoryInfo(string rootPath, string gitDirectoryPath)
{
	public string RootPath { get; } = rootPath;
	public string GitDirectoryPath { get; } = gitDirectoryPath;
}
