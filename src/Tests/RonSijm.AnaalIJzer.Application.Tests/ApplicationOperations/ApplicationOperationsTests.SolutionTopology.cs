namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_InspectsSolutionTopologyOnlyWhenExplicitlyEnabled()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var temporaryDirectory = CreateRepositoryTempDirectory("AnaalIJzer-solution-topology-test");

		try
		{
			var solutionPath = FindRepositoryProject("Examples", "Scenarios", "Example.SolutionTopology", "Example.SolutionTopology.slnx");
			var withoutTopology = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				WriteOutput = false
			}, cancellationToken);
			var jsonOutputPath = Path.Combine(temporaryDirectory, "solution-topology-health.json");
			var withTopology = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				EnforceSolutionTopology = true,
				OutputPath = jsonOutputPath,
				Force = true
			}, cancellationToken);

			withoutTopology.HasFindings.Should().BeFalse();
			withoutTopology.Content.Should().NotContain("ARCH_SOL_001");
			withTopology.HasFindings.Should().BeTrue();
			withTopology.Content.Should().Contain("ARCH_SOL_001");
			withTopology.OutputPath.Should().Be(jsonOutputPath);
			var json = await File.ReadAllTextAsync(jsonOutputPath, cancellationToken);
			json.Should().Contain("\"schemaVersion\": 1");
			json.Should().Contain("\"concern\": \"Solution\"");
			json.Should().Contain("\"reason\": \"NotAllowed\"");
			json.Should().Contain("\"code\": \"ARCH_SOL_001\"");
			json.Should().Contain("Example.SolutionTopology.Application");
		}
		finally
		{
			Directory.Delete(temporaryDirectory, true);
		}
	}
}
