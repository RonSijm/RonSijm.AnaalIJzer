namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsRepositoryIdentity(string canonicalPath, string? gitDirectoryPath)
{
	public string CanonicalPath { get; } = Path.GetFullPath(canonicalPath);
	public string? GitDirectoryPath { get; } = string.IsNullOrWhiteSpace(gitDirectoryPath) ? null : Path.GetFullPath(gitDirectoryPath);
}
