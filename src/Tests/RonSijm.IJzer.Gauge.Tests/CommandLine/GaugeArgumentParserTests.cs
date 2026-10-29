using RonSijm.IJzer.Gauge.CommandLine;

namespace RonSijm.IJzer.Gauge.Tests.CommandLine;

public sealed class GaugeArgumentParserTests
{
	[Fact]
	public void Parse_RecognizesHistoryFlagsAndValues()
	{
		var options = GaugeArgumentParser.Parse(["history", "--repository", "D:\\repo", "--database", "stats.db", "--from-root", "--resume"]);

		options.Command.Should().Be(GaugeCommand.History);
		options.GetValue("--repository").Should().Be("D:\\repo");
		options.HasFlag("--from-root").Should().BeTrue();
		options.HasFlag("--resume").Should().BeTrue();
	}

	[Fact]
	public void Parse_RejectsMissingValue()
	{
		var action = () => GaugeArgumentParser.Parse(["scan", "--project"]);

		action.Should().Throw<ArgumentException>().WithMessage("*requires a value*");
	}

	[Fact]
	public void Parse_RejectsOptionsThatDoNotBelongToTheCommand()
	{
		var action = () => GaugeArgumentParser.Parse(["summary", "--database", "stats.db", "--dimension", "TypeKind"]);

		action.Should().Throw<ArgumentException>().WithMessage("*not valid for 'summary'*");
	}

	[Fact]
	public void Parse_RecognizesTheCommitChangeQuery()
	{
		var options = GaugeArgumentParser.Parse(["commits", "--database", "stats.db", "--dimension", "DependencySite", "--bucket", "Local"]);

		options.Command.Should().Be(GaugeCommand.Commits);
	}

	[Fact]
	public void Parse_RecognizesTheChartCommand()
	{
		var options = GaugeArgumentParser.Parse(["chart", "--database", "stats.db", "--output-directory", "charts", "--dimension", "DependencySite"]);

		options.Command.Should().Be(GaugeCommand.Chart);
		options.GetValue("--dimension").Should().Be("DependencySite");
	}

	[Fact]
	public void Parse_RecognizesTheHistoryChartOptions()
	{
		var options = GaugeArgumentParser.Parse(["chart", "--database", "stats.db", "--output-directory", "charts", "--trend", "--dimension", "DependencySite", "--bucket", "Local"]);

		options.Command.Should().Be(GaugeCommand.Chart);
		options.HasFlag("--trend").Should().BeTrue();
		options.GetValue("--bucket").Should().Be("Local");
	}

	[Fact]
	public void Parse_RecognizesTheGroupedChartOptions()
	{
		var options = GaugeArgumentParser.Parse(["chart", "--database", "stats.db", "--output-directory", "charts", "--group", "--dimension", "MemberAccessibility", "--group-by", "MemberKind"]);

		options.Command.Should().Be(GaugeCommand.Chart);
		options.HasFlag("--group").Should().BeTrue();
		options.GetValue("--group-by").Should().Be("MemberKind");
	}
}
