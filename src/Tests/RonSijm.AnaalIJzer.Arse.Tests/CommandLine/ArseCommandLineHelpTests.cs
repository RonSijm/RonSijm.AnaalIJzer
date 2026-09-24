using System.Text;
using RonSijm.AnaalIJzer.Arse;

namespace RonSijm.AnaalIJzer.Arse.Tests.CommandLine;

public sealed class ArseCommandLineHelpTests
{
    [Fact]
    public async Task RunAsync_CancelledInspection_PropagatesCancellation()
    {
        var projectPath = Path.Combine(GetRepositoryRoot(), "src", "Main", "RonSijm.AnaalIJzer.Core.Statistics", "RonSijm.AnaalIJzer.Core.Statistics.csproj");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => ArseCommandLine.RunAsync(["inspect", "--project", projectPath], cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

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

            var exitCode = await ArseCommandLine.RunAsync(["help"], TestContext.Current.CancellationToken);
            var text = output.ToString();

            exitCode.Should().Be(0);
            text.Should().Contain("arse fixes (--project <project.csproj> | --solution <solution.slnx>)");
            text.Should().Contain("arse apply-fix (--project <project.csproj> | --solution <solution.slnx>) --fix-id <proposal-id>");
            text.Should().Contain("--fix-id");
            text.Should().Contain("--enforce-topology");
            text.Should().Contain("--solution, -s");
            error.ToString().Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var projectPath = Path.Combine(directory.FullName, "src", "Main", "RonSijm.AnaalIJzer.Core.Statistics", "RonSijm.AnaalIJzer.Core.Statistics.csproj");
            if (File.Exists(projectPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}