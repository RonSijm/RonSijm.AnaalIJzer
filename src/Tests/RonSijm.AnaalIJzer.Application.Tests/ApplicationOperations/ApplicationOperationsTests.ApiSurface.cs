namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_GeneratesApiExposureEvidence()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-api-evidence-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject(
				"Examples",
				"Diagnostics",
				"Example.Arch009.ApiSurfaceLeakage",
				"Example.Arch009.ApiSurfaceLeakage.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-documentation.md");
			await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = outputPath,
				IncludeCodeEvidence = true
			}, cancellationToken);

			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("### API Exposure Evidence");
			documentation.Should().Contain("**passes** `Example.Arch009.ApiSurfaceLeakage.CandyOrderingService.OrderProjectedLolly`");
			documentation.Should().Contain("**violates** `Example.Arch009.ApiSurfaceLeakage.CandyOrderingService.OrderRawLolly`");
			documentation.Should().Contain("RepositoryQuerySurface");
			documentation.Should().Contain("MethodReturn");
			documentation.Should().Contain("`ARCH009`");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_GeneratesTransitiveApiExposureEvidence()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-transitive-api-evidence-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject(
				"Examples",
				"Diagnostics",
				"Example.Arch014.TransitiveExposure",
				"Example.Arch014.TransitiveExposure.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-documentation.md");
			await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = outputPath,
				IncludeCodeEvidence = true
			}, cancellationToken);

			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("### API Exposure Evidence");
			documentation.Should().Contain("CandyOrderingService.OrderRawLolly");
			documentation.Should().Contain("CandyReceipt.RawQuery");
			documentation.Should().Contain("LollyQueryable");
			documentation.Should().Contain("at depth `1`");
			documentation.Should().Contain("`ARCH014`");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}
}

