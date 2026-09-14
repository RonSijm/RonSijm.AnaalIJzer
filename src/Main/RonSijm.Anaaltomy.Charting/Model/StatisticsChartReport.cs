namespace RonSijm.Anaaltomy.Charting.Model;

public sealed class StatisticsChartReport(IReadOnlyList<string> outputPaths)
{
	public IReadOnlyList<string> OutputPaths { get; } = outputPaths;
}
