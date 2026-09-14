namespace RonSijm.AnaalIJzer.Statistics.Roslyn.Tests.Analysis;

public sealed class CompilationStatisticsCollectorTests
{
	[Fact]
	public void Collect_RecordsTypeMemberAccessibilityAndSiteStatistics()
	{
		var compilation = CreateCompilation("""
		                                    using System;
		                                    [Serializable]
		                                    public record CustomerRecord;
		                                    internal interface IKitchen { }
		                                    public sealed class PizzaKitchen : IKitchen
		                                    {
		                                        private readonly IKitchen kitchen;
		                                        public PizzaKitchen(IKitchen kitchen) { this.kitchen = kitchen; }
		                                        public IKitchen Cook(IKitchen input)
		                                        {
		                                            var local = new PizzaKitchen(input);
		                                            return local.kitchen;
		                                        }
		                                        public event EventHandler? Cooked;
		                                    }
		                                    """);
		var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");

		var result = CompilationStatisticsCollector.Collect(compilation, identity, TestContext.Current.CancellationToken);

		GetCount(result, StatisticsDimension.TypeKind, "Record").Should().Be(1);
		GetCount(result, StatisticsDimension.TypeKind, "Interface").Should().Be(1);
		GetCount(result, StatisticsDimension.TypeKind, "Class").Should().Be(1);
		GetCount(result, StatisticsDimension.MemberKind, "Constructor").Should().Be(1);
		GetCount(result, StatisticsDimension.MemberKind, "Method").Should().Be(1);
		GetCount(result, StatisticsDimension.MemberKind, "Field").Should().Be(1);
		GetCount(result, StatisticsDimension.MemberKind, "Event").Should().Be(1);
		GetGroupedCount(result, StatisticsDimension.TypeAccessibility, "Public", StatisticsDimension.TypeKind, "Class").Should().Be(1);
		GetGroupedCount(result, StatisticsDimension.MemberAccessibility, "Public", StatisticsDimension.MemberKind, "Method").Should().Be(1);
		GetGroupedCount(result, StatisticsDimension.MemberAccessibility, "Private", StatisticsDimension.MemberKind, "Field").Should().Be(1);
		GetCount(result, StatisticsDimension.DependencySite, "Constructor").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.DependencySite, "Local").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.DependencySite, "New").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.DependencySite, "Attribute").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.DependencySite, "InterfaceImplementation").Should().BeGreaterThan(0);
		result.SourceFileCount.Should().Be(1);
	}

	[Fact]
	public void Collect_ExcludesGeneratedTypesByDefault()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var compilation = CSharpCompilation.Create(
			"Generated",
			[
				CSharpSyntaxTree.ParseText(SourceText.From("public class PizzaKitchen { }"), path: "PizzaKitchen.cs", cancellationToken: cancellationToken),
				CSharpSyntaxTree.ParseText(SourceText.From("public class GeneratedPizzaKitchen { }"), path: "GeneratedPizzaKitchen.g.cs", cancellationToken: cancellationToken)
			],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
		var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");

		var result = CompilationStatisticsCollector.Collect(compilation, identity, TestContext.Current.CancellationToken);

		GetCount(result, StatisticsDimension.TypeKind, "Class").Should().Be(1);
		result.SourceFileCount.Should().Be(1);
	}

	[Fact]
	public void Collect_NormalizesEverySupportedTypeKindAccessibilityAndMemberKind()
	{
		var compilation = CreateCompilation("""
		                                    file class FileScopedType { }
		                                    public class PublicClass
		                                    {
		                                        public int PublicField;
		                                        internal int InternalField;
		                                        protected int ProtectedField;
		                                        protected internal int ProtectedInternalField;
		                                        private protected int PrivateProtectedField;
		                                        private int PrivateField;
		                                        public PublicClass() { }
		                                        public void PublicMethod() { }
		                                    }
		                                    internal struct InternalStruct { }
		                                    public record PublicRecord;
		                                    internal record struct InternalRecordStruct;
		                                    public interface IContract { }
		                                    public enum PizzaKind { Plain }
		                                    public delegate void PizzaAction();
		                                    """);
		var identity = new StatisticsProjectIdentity("Pizza.csproj", "Pizza", "Pizza", "net10.0");

		var result = CompilationStatisticsCollector.Collect(compilation, identity, TestContext.Current.CancellationToken);

		foreach (var typeKind in StatisticsDimensionCatalog.TypeKinds.Where(typeKind => typeKind != StatisticsDimensionCatalog.Other))
		{
			GetCount(result, StatisticsDimension.TypeKind, typeKind).Should().BeGreaterThan(0, typeKind + " should be counted.");
		}

		foreach (var accessibility in StatisticsDimensionCatalog.Accessibilities.Where(accessibility => accessibility is not StatisticsDimensionCatalog.NotApplicable and not "File"))
		{
			GetCount(result, StatisticsDimension.MemberAccessibility, accessibility).Should().BeGreaterThan(0, accessibility + " member accessibility should be counted.");
		}

		GetCount(result, StatisticsDimension.TypeAccessibility, "Public").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.TypeAccessibility, "Internal").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.TypeAccessibility, "File").Should().Be(1);
		GetCount(result, StatisticsDimension.MemberKind, "Constructor").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.MemberKind, "Method").Should().BeGreaterThan(0);
		GetCount(result, StatisticsDimension.MemberKind, "Field").Should().BeGreaterThan(0);
	}

	private static long GetCount(StatisticsProjectSnapshot snapshot, StatisticsDimension dimension, string bucket)
	{
		var result = snapshot.Measurements.SingleOrDefault(measurement => measurement.Dimension == dimension && measurement.Bucket == bucket).Count;

		return result;
	}

	private static long GetGroupedCount(StatisticsProjectSnapshot snapshot, StatisticsDimension dimension, string bucket, StatisticsDimension groupDimension, string groupBucket)
	{
		var result = snapshot.GroupedMeasurements.SingleOrDefault(measurement => measurement.Dimension == dimension
			&& measurement.Bucket == bucket
			&& measurement.GroupDimension == groupDimension
			&& measurement.GroupBucket == groupBucket).Count;

		return result;
	}

	private static CSharpCompilation CreateCompilation(string source)
	{
		var result = CSharpCompilation.Create(
			"Statistics",
			[CSharpSyntaxTree.ParseText(SourceText.From(source), path: "Statistics.cs")],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

		return result;
	}
}
