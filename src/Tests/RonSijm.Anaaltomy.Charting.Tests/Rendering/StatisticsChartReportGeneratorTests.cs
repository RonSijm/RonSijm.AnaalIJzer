using RonSijm.AnaalIJzer.Core.Statistics.Model;
using RonSijm.Anaaltomy.Charting.Model;
using RonSijm.Anaaltomy.Charting.Rendering;

namespace RonSijm.Anaaltomy.Charting.Tests.Rendering;

public sealed class StatisticsChartReportGeneratorTests
{
	[Fact]
	public void Generate_WritesOnePngForEachPopulatedDimension()
	{
		var directoryPath = CreateTemporaryDirectory();
		try
		{
			var generator = new StatisticsChartReportGenerator();
			var report = generator.Generate(directoryPath,
			[
				new StatisticsMeasurement(StatisticsDimension.TypeKind, "Class", 42),
				new StatisticsMeasurement(StatisticsDimension.TypeKind, "Interface", 7),
				new StatisticsMeasurement(StatisticsDimension.DependencySite, "Method", 18)
			], subject: "Azure.Storage.Blobs");

			report.OutputPaths.Should().BeEquivalentTo(
			[
				Path.Combine(directoryPath, "type-kinds.png"),
				Path.Combine(directoryPath, "dependency-sites.png")
			]);
			foreach (var outputPath in report.OutputPaths)
			{
				File.Exists(outputPath).Should().BeTrue();
				ReadPngSignature(outputPath).Should().Equal(137, 80, 78, 71, 13, 10, 26, 10);
			}
		}
		finally
		{
			DeleteTemporaryDirectory(directoryPath);
		}
	}

	[Fact]
	public void Generate_WithRequestedDimension_WritesOnlyThatDimension()
	{
		var directoryPath = CreateTemporaryDirectory();
		try
		{
			var generator = new StatisticsChartReportGenerator();
			var report = generator.Generate(directoryPath,
			[
				new StatisticsMeasurement(StatisticsDimension.TypeKind, "Class", 42),
				new StatisticsMeasurement(StatisticsDimension.DependencySite, "Method", 18)
			],
			StatisticsDimension.DependencySite);

			report.OutputPaths.Should().ContainSingle().Which.Should().EndWith("dependency-sites.png");
			Directory.GetFiles(directoryPath, "*.png").Should().ContainSingle();
		}
		finally
		{
			DeleteTemporaryDirectory(directoryPath);
		}
	}

	[Fact]
	public void GenerateTrend_WritesAPngWithTheRequestedDimensionAndBucket()
	{
		var directoryPath = CreateTemporaryDirectory();
		try
		{
			var generator = new StatisticsChartReportGenerator();
			var report = generator.GenerateTrend(directoryPath, StatisticsDimension.DependencySite, "Local",
			[
				new StatisticsTrendChartPoint("aaa11111", DateTimeOffset.Parse("2026-01-01T00:00:00Z"), 12),
				new StatisticsTrendChartPoint("bbb22222", DateTimeOffset.Parse("2026-01-02T00:00:00Z"), 18)
			]);

			report.OutputPaths.Should().ContainSingle().Which.Should().EndWith("dependency-sites-local-trend.png");
			ReadPngSignature(report.OutputPaths[0]).Should().Equal(137, 80, 78, 71, 13, 10, 26, 10);
		}
		finally
		{
			DeleteTemporaryDirectory(directoryPath);
		}
	}

	[Fact]
	public void GenerateGrouped_WritesAPngWithTheRequestedDimensionAndGroupDimension()
	{
		var directoryPath = CreateTemporaryDirectory();
		try
		{
			var generator = new StatisticsChartReportGenerator();
			var report = generator.GenerateGrouped(directoryPath,
			[
				new StatisticsGroupedMeasurement(StatisticsDimension.MemberAccessibility, "Public", StatisticsDimension.MemberKind, "Method", 10),
				new StatisticsGroupedMeasurement(StatisticsDimension.MemberAccessibility, "Private", StatisticsDimension.MemberKind, "Field", 5)
			],
			StatisticsDimension.MemberAccessibility,
			StatisticsDimension.MemberKind);

			report.OutputPaths.Should().ContainSingle().Which.Should().EndWith("member-accessibility-by-member-kinds.png");
			ReadPngSignature(report.OutputPaths[0]).Should().Equal(137, 80, 78, 71, 13, 10, 26, 10);
		}
		finally
		{
			DeleteTemporaryDirectory(directoryPath);
		}
	}

	private static string CreateTemporaryDirectory()
	{
		var result = Path.Combine(Path.GetTempPath(), "Anaaltomy", "Charts", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(result);

		return result;
	}

	private static byte[] ReadPngSignature(string path)
	{
		using var stream = File.OpenRead(path);
		var signature = new byte[8];
		var bytesRead = stream.Read(signature, 0, signature.Length);
		bytesRead.Should().Be(signature.Length);

		return signature;
	}

	private static void DeleteTemporaryDirectory(string directoryPath)
	{
		if (Directory.Exists(directoryPath))
		{
			Directory.Delete(directoryPath, true);
		}
	}
}
