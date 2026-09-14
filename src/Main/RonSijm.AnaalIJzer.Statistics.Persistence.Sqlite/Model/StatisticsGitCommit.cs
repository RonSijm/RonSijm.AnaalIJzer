namespace RonSijm.AnaalIJzer.Statistics.Persistence.Sqlite.Model;

public sealed class StatisticsGitCommit(
	string sha,
	string treeSha,
	DateTimeOffset authoredAtUtc,
	DateTimeOffset committedAtUtc,
	IReadOnlyList<string> parentShas,
	string? subject = null,
	string? authorName = null)
{
	public string Sha { get; } = sha;
	public string TreeSha { get; } = treeSha;
	public DateTimeOffset AuthoredAtUtc { get; } = authoredAtUtc;
	public DateTimeOffset CommittedAtUtc { get; } = committedAtUtc;
	public IReadOnlyList<string> ParentShas { get; } = parentShas;
	public string? Subject { get; } = subject;
	public string? AuthorName { get; } = authorName;
}
