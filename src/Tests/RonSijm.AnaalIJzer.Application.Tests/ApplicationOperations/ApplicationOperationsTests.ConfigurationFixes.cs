namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_FindsAndAppliesConfigurationFixes_ForXmlSettingsProject()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("config-fixes-xml");

		try
		{
			var projectPath = CloneExampleProject(
				tempDirectory,
				"Features",
				"Example.IncludeSettings",
				"Example.IncludeSettings.csproj");
			var configPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Architecture.anl");
			var runner = new ApplicationRunner();

			var fixesResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			fixesResult.Message.Should().Contain("Found");
			fixesResult.Content.Should().Contain("# Architecture Configuration Fixes");
			var fix = fixesResult.FixProposals.Single(proposal => proposal.Title == "Add allowed dependency 'Presentation' -> 'Persistence'");
			fix.DiagnosticId.Should().Be("ARCH001");
			fix.TargetPath.Should().Be(configPath);
			fix.PreviewDiff.Should().Contain("<AllowedDependency from=\"Presentation\" to=\"Persistence\" />");

			var applyResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ApplyFix)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				FixId = fix.Id
			}, cancellationToken);

			applyResult.Message.Should().Contain("Applied " + fix.Title);
			var updatedConfig = await File.ReadAllTextAsync(configPath, cancellationToken);
			updatedConfig.Should().Contain("<AllowedDependency from=\"Presentation\" to=\"Persistence\" />");

			var remainingResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			remainingResult.FixProposals.Should().BeEmpty();
			remainingResult.Content.Should().Contain("No configuration-backed fix proposals are currently available");
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}

	[Fact]
	public async Task ApplicationRunner_FindsAndAppliesConfigurationFixes_ForInlineSettingsProject()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("config-fixes-inline");

		try
		{
			var projectPath = CloneExampleProject(
				tempDirectory,
				"Features",
				"Example.InlineXml",
				"Example.InlineXml.csproj");
			var sourcePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Example.cs");
			var runner = new ApplicationRunner();

			var fixesResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			fixesResult.Message.Should().Contain("Found");
			fixesResult.Content.Should().Contain("# Architecture Configuration Fixes");
			var fix = fixesResult.FixProposals.Single(proposal => proposal.Title == "Add allowed dependency 'Presentation' -> 'Persistence'");
			fix.DiagnosticId.Should().Be("ARCH001");
			fix.TargetPath.Should().Be(sourcePath);
			fix.PreviewDiff.Should().Contain("<AllowedDependency from=\"Presentation\" to=\"Persistence\" />");

			var applyResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ApplyFix)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				FixId = fix.Id
			}, cancellationToken);

			applyResult.Message.Should().Contain("Applied " + fix.Title);
			var updatedSource = await File.ReadAllTextAsync(sourcePath, cancellationToken);
			updatedSource.Should().Contain("<AllowedDependency from=\"Presentation\" to=\"Persistence\" />");
			updatedSource.Should().Contain("AssemblyMetadata(\"AnaalIJzerSettings\"");

			var remainingResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			remainingResult.FixProposals.Should().BeEmpty();
			remainingResult.Content.Should().Contain("No configuration-backed fix proposals are currently available");
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}

	[Fact]
	public async Task ApplicationRunner_FindsAndAppliesConfigurationFixes_ForSolutionInput()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("config-fixes-solution");

		try
		{
			var projectPath = CloneExampleProject(
				tempDirectory,
				"Features",
				"Example.IncludeSettings",
				"Example.IncludeSettings.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var configPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Architecture.anl");
			var runner = new ApplicationRunner();

			var fixesResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				WriteOutput = false
			}, cancellationToken);

			fixesResult.Message.Should().Contain("Found");
			var fix = fixesResult.FixProposals.Single(proposal => proposal.Title == "Add allowed dependency 'Presentation' -> 'Persistence'");
			fix.TargetPath.Should().Be(configPath);

			var applyResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ApplyFix)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				FixId = fix.Id
			}, cancellationToken);

			applyResult.Message.Should().Contain("Applied " + fix.Title);
			var updatedConfig = await File.ReadAllTextAsync(configPath, cancellationToken);
			updatedConfig.Should().Contain("<AllowedDependency from=\"Presentation\" to=\"Persistence\" />");

			var remainingResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Solution,
				InputPaths = [solutionPath],
				WriteOutput = false
			}, cancellationToken);

			remainingResult.FixProposals.Should().BeEmpty();
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}

	[Fact]
	public async Task ApplicationRunner_FindsAndAppliesWrongDirectionFlipFix_ForInlineSettingsProject_AndReportsOppositeUsage()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("config-fixes-arch004-inline");

		try
		{
			var projectPath = CloneExampleProject(
				tempDirectory,
				"Diagnostics",
				"Example.Arch004.WrongDirection",
				"Example.Arch004.WrongDirection.csproj");
			var sourcePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Example.cs");
			var runner = new ApplicationRunner();

			var fixesResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			var fix = fixesResult.FixProposals.Single(proposal => proposal.Title == "Flip configured dependency 'Chef' -> 'Pantry' to 'Pantry' -> 'Chef'");
			fix.DiagnosticId.Should().Be("ARCH004");
			fix.TargetPath.Should().Be(sourcePath);
			fix.PreviewDiff.Should().Contain("<AllowedDependency from=\"Pantry\" to=\"Chef\" />");

			var applyResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ApplyFix)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				FixId = fix.Id
			}, cancellationToken);

			applyResult.Message.Should().Contain("Applied " + fix.Title);
			var updatedSource = await File.ReadAllTextAsync(sourcePath, cancellationToken);
			updatedSource.Should().Contain("<AllowedDependency from=\"Pantry\" to=\"Chef\" />");
			updatedSource.Should().NotContain("<AllowedDependency from=\"Chef\" to=\"Pantry\" />");

			var remainingResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			remainingResult.FixProposals.Should().ContainSingle(proposal => proposal.Title == "Add allowed dependency 'Chef' -> 'Pantry'");
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}

	[Fact]
	public async Task ApplicationRunner_FindsAndAppliesConfiguredCycleFix_ForInlineSettingsProject()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("config-fixes-arch007-inline");

		try
		{
			var projectPath = CloneExampleProject(
				tempDirectory,
				"Diagnostics",
				"Example.Arch007.CyclicGraph",
				"Example.Arch007.CyclicGraph.csproj");
			var sourcePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Example.cs");
			var runner = new ApplicationRunner();

			var fixesResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			var fix = fixesResult.FixProposals.Single(proposal => proposal.Title == "Break configured cycle by blocking 'Ordering' -> 'Inventory'");
			fix.DiagnosticId.Should().Be("ARCH007");
			fix.TargetPath.Should().Be(sourcePath);
			fix.Risk.Should().Be("HighRisk");
			fix.PreviewDiff.Should().Contain("<BlockedDependency from=\"Ordering\" to=\"Inventory\" />");

			var applyResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ApplyFix)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				FixId = fix.Id
			}, cancellationToken);

			applyResult.Message.Should().Contain("Applied " + fix.Title);
			var updatedSource = await File.ReadAllTextAsync(sourcePath, cancellationToken);
			updatedSource.Should().Contain("<BlockedDependency from=\"Ordering\" to=\"Inventory\" />");

			var remainingResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Fixes)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);

			remainingResult.FixProposals.Should().BeEmpty();
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}
}
