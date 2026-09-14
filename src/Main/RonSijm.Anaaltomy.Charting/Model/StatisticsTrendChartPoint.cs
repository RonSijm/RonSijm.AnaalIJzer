namespace RonSijm.Anaaltomy.Charting.Model;

public sealed class StatisticsTrendChartPoint(string commitSha, DateTimeOffset? committedAtUtc, long count)
{
	public string CommitSha { get; } = commitSha;
	public DateTimeOffset? CommittedAtUtc { get; } = committedAtUtc;
	public long Count { get; } = count;
}
