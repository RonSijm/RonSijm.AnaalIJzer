namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_InspectsReleaseProjectWithoutRunningSourceLink()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("AnaalIJzer-inspect-test");

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var projectPath = Path.Combine(repositoryRoot, "src", "Main", "RonSijm.AnaalIJzer", "RonSijm.AnaalIJzer.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-health.md");
			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				Configuration = "Release",
				WriteOutput = false
			}, cancellationToken);

			result.Message.Should().Contain("Architecture inspection");
			result.Content.Should().Contain("# Architecture Health");
			result.OutputPath.Should().Be(Path.Combine(Path.GetDirectoryName(projectPath)!, "architecture-health.md"));
			File.Exists(outputPath).Should().BeFalse();
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_InspectsSolutionWithoutWritingOutput()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("AnaalIJzer-inspect-solution-test");

		try
		{
			var projectPath = CloneExampleProject(tempDirectory, "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				Configuration = "Release",
				WriteOutput = false
			}, cancellationToken);

			result.Message.Should().Contain("Architecture inspection");
			result.Content.Should().Contain("# Architecture Health");
			result.Content.Should().Contain("**Solution**");
			result.Content.Should().Contain("Example.RepositoryQuerySurface");
			result.OutputPath.Should().Be(Path.Combine(tempDirectory, "architecture-health.md"));
			File.Exists(result.OutputPath).Should().BeFalse();
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_GeneratesSolutionViolationReport()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("AnaalIJzer-report-solution-test");

		try
		{
			var projectPath = CloneExampleProject(tempDirectory, "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var outputPath = Path.Combine(tempDirectory, "architectural-violations.md");
			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Report)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				OutputPath = outputPath,
				Configuration = "Release"
			}, cancellationToken);

			result.OutputPath.Should().Be(outputPath);
			var report = await File.ReadAllTextAsync(outputPath, cancellationToken);
			report.Should().Contain("# Architectural Violation Report");
			report.Should().Contain("**Solution**");
			report.Should().Contain("ARCH001");
			report.Should().Contain("OrderDashboardService");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_HealthInspectionEvaluatesEveryCombinedMatcherCondition()
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
		var projectPath = Path.Combine(repositoryRoot, "Examples", "Features", "Example.CombinedMatchers", "Example.CombinedMatchers.csproj");
		var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
		{
			InputKind = ApplicationInputKind.Project,
			InputPaths = [projectPath],
			WriteOutput = false
		}, TestContext.Current.CancellationToken);

		result.Content.Should().Contain("ARCH005");
		result.Content.Should().NotContain("| Warning | Unmatched matcher |");
	}

	[Fact]
	public async Task ApplicationRunner_ValidatesConfigurationAndReportsPermittedCycles()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("AnaalIJzer-health-test");

		try
		{
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var outputPath = Path.Combine(tempDirectory, "architecture-health.md");
			await File.WriteAllTextAsync(configPath, """
			                                         <ArchitecturalLevels>
			                                           <Layer name="Application"><Class endsWith="Service" /></Layer>
			                                           <Layer name="Repository"><Class endsWith="Repository" /></Layer>
			                                           <AllowedDependency from="Application" to="Repository" />
			                                           <AllowedDependency from="Repository" to="Application" />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);

			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputPath
			}, cancellationToken);

			result.HasFindings.Should().BeTrue();
			result.Message.Should().Contain("found 1 issue");
			var report = await File.ReadAllTextAsync(outputPath, cancellationToken);
			report.Should().Contain("# Architecture Health");
			report.Should().Contain("Configured cycle");
			report.Should().Contain("Application -> Repository -> Application");
			report.Should().Contain("enforceAcyclic is disabled");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_Inspection_PreservesObservedCycleWarning_WhenObservedEnforcementIsDisabled()
	{
		var tempDirectory = CreateRepositoryTempDirectory("AnaalIJzer-observed-cycle-warning");

		try
		{
			var sourceDirectory = FindRepositoryProject("Examples", "Diagnostics", "Example.Arch018.ObservedCycle").TrimEnd(Path.DirectorySeparatorChar);
			var clonedExamplesDirectory = Path.Combine(tempDirectory, "Examples");
			Directory.CreateDirectory(clonedExamplesDirectory);
			File.Copy(
				FindRepositoryProject("Examples", "Directory.Build.props"),
				Path.Combine(clonedExamplesDirectory, "Directory.Build.props"),
				overwrite: true);
			var clonedDirectory = Path.Combine(clonedExamplesDirectory, "Example.Arch018.ObservedCycle");
			CopyDirectory(sourceDirectory, clonedDirectory);

			var configPath = Path.Combine(clonedDirectory, "Architecture.anl");
			var configText = await File.ReadAllTextAsync(configPath, TestContext.Current.CancellationToken);
			configText = configText.Replace(" enforceObservedAcyclic=\"true\"", string.Empty, StringComparison.Ordinal);
			await File.WriteAllTextAsync(configPath, configText, TestContext.Current.CancellationToken);

			var projectPath = Path.Combine(clonedDirectory, "Example.Arch018.ObservedCycle.csproj");
			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, TestContext.Current.CancellationToken);

			result.Content.Should().Contain("| Warning | Observed dependency cycle |");
			result.Content.Should().Contain("Notifications -> Ordering -> Notifications");
			result.Content.Should().NotContain("| Error | ARCH018 |");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_Inspection_PromotesObservedCycleToArch018_WhenObservedEnforcementIsEnabled()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var projectPath = FindRepositoryProject("Examples", "Diagnostics", "Example.Arch018.ObservedCycle", "Example.Arch018.ObservedCycle.csproj");
		var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
		{
			InputKind = ApplicationInputKind.Project,
			InputPaths = [projectPath],
			WriteOutput = false
		}, cancellationToken);

		result.Content.Should().Contain("| Error | ARCH018 |");
		result.Content.Should().NotContain("| Warning | Observed dependency cycle |");
		result.Content.Should().Contain("Notifications -> Ordering -> Notifications");
	}
}

