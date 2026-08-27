using RonSijm.AnaalIJzer.Application;

namespace RonSijm.AnaalIJzer.Arse.Tests.CommandLine;

public sealed class CommandOptionsTests
{
	[Fact]
	public void Parse_AcceptsSolutionInput()
	{
		var options = CommandOptions.Parse(["--solution", "src\\MyApp.slnx", "--output", "docs\\architecture-health.md"]);
		var request = options.ToRequest(ApplicationOperationKind.Inspect);

		request.InputKind.Should().Be(ApplicationInputKind.Solution);
		request.InputPaths.Should().Equal("src\\MyApp.slnx");
		request.OutputPath.Should().Be("docs\\architecture-health.md");
	}

	[Fact]
	public void Parse_AcceptsHelpfulGenerationStrategy()
	{
		var options = CommandOptions.Parse(["--project", "src\\MyApp.csproj", "--strategy", "helpful"]);
		var request = options.ToRequest(ApplicationOperationKind.GenerateConfig);

		request.GenerationOptions.Strategy.Should().Be(ConfigurationGenerationStrategy.Helpful);
	}

	[Fact]
	public void Parse_RejectsMixedProjectAndSolutionInput()
	{
		var parse = () => CommandOptions.Parse(["--project", "src\\MyApp.csproj", "--solution", "src\\MyApp.slnx"]);

		parse.Should().Throw<Exception>().WithMessage("Use only one input option.");
	}
}
