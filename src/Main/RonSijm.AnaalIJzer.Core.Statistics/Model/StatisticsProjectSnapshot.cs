namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public sealed class StatisticsProjectSnapshot(
	StatisticsProjectIdentity identity,
	IReadOnlyList<StatisticsMeasurement> measurements,
	int sourceFileCount,
	int typeCount,
	int memberCount,
	int compilerErrorCount,
	int unresolvedObservationCount,
	IReadOnlyList<StatisticsGroupedMeasurement>? groupedMeasurements = null)
{
	public StatisticsProjectIdentity Identity { get; } = identity;
	public IReadOnlyList<StatisticsMeasurement> Measurements { get; } = measurements;
	public IReadOnlyList<StatisticsGroupedMeasurement> GroupedMeasurements { get; } = groupedMeasurements ?? [];
	public int SourceFileCount { get; } = sourceFileCount;
	public int TypeCount { get; } = typeCount;
	public int MemberCount { get; } = memberCount;
	public int CompilerErrorCount { get; } = compilerErrorCount;
	public int UnresolvedObservationCount { get; } = unresolvedObservationCount;
}
