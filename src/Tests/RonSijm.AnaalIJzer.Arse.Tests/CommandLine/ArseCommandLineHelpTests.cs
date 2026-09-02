using System.Text;
using RonSijm.AnaalIJzer.Arse;

namespace RonSijm.AnaalIJzer.Arse.Tests.CommandLine;

public sealed class ArseCommandLineHelpTests
{
	[Fact]
	public async Task RunAsync_Help_PrintsFixCommandsAndSolutionInputs()
	{
		var originalOut = Console.Out;
		var originalError = Console.Error;
		using var output = new StringWriter(new StringBuilder());
		using var error = new StringWriter(new StringBuilder());

		try
		{
			Console.SetOut(output);
			Console.SetError(error);

			var exitCode = await ArseCommandLine.RunAsync(["help"]);
			var text = output.ToString();

			exitCode.Should().Be(0);
			text.Should().Contain("arse fixes (--project <project.csproj> | --solution <solution.slnx>)");
			text.Should().Contain("arse apply-fix (--project <project.csproj> | --solution <solution.slnx>) --fix-id <proposal-id>");
			text.Should().Contain("--fix-id");
			text.Should().Contain("--solution, -s");
			error.ToString().Should().BeEmpty();
		}
		finally
		{
			Console.SetOut(originalOut);
			Console.SetError(originalError);
		}
	}
}
