namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

public sealed class GitHistorySelection(bool fromRoot, string? fromRevision = null, string? toRevision = null, bool firstParent = false, int? maximumCommitCount = null, bool includeCommitMetadata = false)
{
	public bool FromRoot { get; } = fromRoot;
	public string? FromRevision { get; } = fromRevision;
	public string? ToRevision { get; } = toRevision;
	public bool FirstParent { get; } = firstParent;
	public int? MaximumCommitCount { get; } = maximumCommitCount;
	public bool IncludeCommitMetadata { get; } = includeCommitMetadata;

	public void Validate()
	{
		if (FromRoot && !string.IsNullOrWhiteSpace(FromRevision))
		{
			throw new ArgumentException("Use either --from-root or --from, not both.");
		}

		if (!FromRoot && string.IsNullOrWhiteSpace(FromRevision))
		{
			throw new ArgumentException("Select a revision range with --from or explicitly use --from-root.");
		}

		if (MaximumCommitCount is <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(MaximumCommitCount), "The maximum commit count must be greater than zero.");
		}
	}
}
