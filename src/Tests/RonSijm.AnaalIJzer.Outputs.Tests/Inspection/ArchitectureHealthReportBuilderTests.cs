using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Outputs.Inspection;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Inspection;

public sealed class ArchitectureHealthReportBuilderTests
{
	[Fact]
	public void Builder_RendersSummaryAndTableForFindings()
	{
		var findings = new[]
		{
			new ArchitectureFinding(ArchitectureFindingSeverity.Warning, "Rule", "A warning finding", "ProjectA"),
			new ArchitectureFinding(ArchitectureFindingSeverity.Error, "ARCH001", "An error finding", "ProjectB")
		};

		var report = ArchitectureHealthReportBuilder.Build("MyProject", findings, @"D:\temp\MyProject.csproj");

		report.FindingCount.Should().Be(2);
		report.Markdown.Should().Contain("# Architecture Health");
		report.Markdown.Should().Contain("**Input**: `MyProject`");
		report.Markdown.Should().Contain("**Project**: `D:\\temp\\MyProject.csproj`");
		report.Markdown.Should().Contain("**Findings**: 1 error(s), 1 warning(s)");
		report.Markdown.Should().Contain("| Error | ARCH001 | An error finding | ProjectB |");
		report.Markdown.Should().Contain("| Warning | Rule | A warning finding | ProjectA |");
	}

	[Fact]
	public void Builder_RendersEmptyStateWhenThereAreNoFindings()
	{
		var report = ArchitectureHealthReportBuilder.Build("MyProject", Array.Empty<ArchitectureFinding>(), null);

		report.FindingCount.Should().Be(0);
		report.Markdown.Should().Contain("No configuration, classification, dependency-graph, or rule-usage problems were found.");
		report.Findings.Should().BeEmpty();
	}
}
