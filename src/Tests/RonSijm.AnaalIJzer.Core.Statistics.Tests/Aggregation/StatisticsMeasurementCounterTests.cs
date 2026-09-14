namespace RonSijm.AnaalIJzer.Core.Statistics.Tests.Aggregation;

public sealed class StatisticsMeasurementCounterTests
{
	[Fact]
	public void GetMeasurements_OrdersKnownBucketsBeforeUnknownBuckets()
	{
		var counter = new StatisticsMeasurementCounter();
		counter.Record(StatisticsDimension.DependencySite, "UnconfiguredSite");
		counter.Record(StatisticsDimension.DependencySite, "Local", 2);
		counter.Record(StatisticsDimension.TypeKind, "Record");
		counter.Record(StatisticsDimension.TypeKind, "Class", 3);

		var result = counter.GetMeasurements();

		result.Should().BeEquivalentTo(
		[
			new StatisticsMeasurement(StatisticsDimension.TypeKind, "Class", 3),
			new StatisticsMeasurement(StatisticsDimension.TypeKind, "Record", 1),
			new StatisticsMeasurement(StatisticsDimension.DependencySite, "Local", 2),
			new StatisticsMeasurement(StatisticsDimension.DependencySite, "UnconfiguredSite", 1)
		], options => options.WithStrictOrdering());
	}

	[Fact]
	public void GetGroupedMeasurements_OrdersKnownBucketsBeforeUnknownBuckets()
	{
		var counter = new StatisticsMeasurementCounter();
		counter.RecordGrouped(StatisticsDimension.MemberAccessibility, "Private", StatisticsDimension.MemberKind, "Field", 2);
		counter.RecordGrouped(StatisticsDimension.MemberAccessibility, "Public", StatisticsDimension.MemberKind, "Method", 3);
		counter.RecordGrouped(StatisticsDimension.TypeAccessibility, "Internal", StatisticsDimension.TypeKind, "Class");

		var result = counter.GetGroupedMeasurements();

		result.Should().BeEquivalentTo(
		[
			new StatisticsGroupedMeasurement(StatisticsDimension.TypeAccessibility, "Internal", StatisticsDimension.TypeKind, "Class", 1),
			new StatisticsGroupedMeasurement(StatisticsDimension.MemberAccessibility, "Public", StatisticsDimension.MemberKind, "Method", 3),
			new StatisticsGroupedMeasurement(StatisticsDimension.MemberAccessibility, "Private", StatisticsDimension.MemberKind, "Field", 2)
		], options => options.WithStrictOrdering());
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Record_RejectsNonPositiveCounts(long count)
	{
		var counter = new StatisticsMeasurementCounter();

		var action = () => counter.Record(StatisticsDimension.TypeKind, "Class", count);

		action.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void RecordGrouped_RejectsNonPositiveCounts(long count)
	{
		var counter = new StatisticsMeasurementCounter();

		var action = () => counter.RecordGrouped(StatisticsDimension.TypeKind, "Class", StatisticsDimension.TypeAccessibility, "Public", count);

		action.Should().Throw<ArgumentOutOfRangeException>();
	}
}
