using AwesomeAssertions;
using RonSijm.AnaalIJzer.Application;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests;

public sealed class ExampleApplicationIntegrationTests
{
	[Fact]
	public async Task ApplicationRunner_MergesEveryExampleConfigurationAndGeneratesExtensiveDocumentation()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var context = ExampleRepositoryContext.Discover();
		var projectPaths = context.FindAllExampleProjectPaths();
		var standaloneConfigurationPaths = context.FindStandaloneConfigurationExamples();
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-all-examples-application-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		using var generatedFiles = new GeneratedExampleFilesScope(context);

		try
		{
			projectPaths.Should().NotBeEmpty("the repository should keep runnable example projects under Examples");

			var runner = new ApplicationRunner();
			var configurationPaths = new List<string>();
			var projectDocumentationDirectory = Path.Combine(tempDirectory, "ProjectDocumentation");
			var standaloneDocumentationDirectory = Path.Combine(tempDirectory, "StandaloneConfigurationDocumentation");
			Directory.CreateDirectory(projectDocumentationDirectory);
			Directory.CreateDirectory(standaloneDocumentationDirectory);

			foreach (var projectPath in projectPaths)
			{
				var relativeProjectPath = Path.GetRelativePath(context.ExamplesRoot, projectPath);
				try
				{
					var configurationPath = await ExampleApplicationOperations.GetXmlConfigurationForMergeAsync(runner, projectPath, relativeProjectPath, tempDirectory, cancellationToken);
					configurationPaths.Add(configurationPath);

					var documentationPath = Path.Combine(projectDocumentationDirectory, Path.ChangeExtension(context.SanitizePath(relativeProjectPath), ".md"));
					await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
					{
						InputKind = ApplicationInputKind.Project,
						InputPaths = [projectPath],
						OutputPath = documentationPath,
						IncludeCodeEvidence = true,
						IncludeDocumentationInput = true,
						Force = true
					}, cancellationToken);

					var documentation = await File.ReadAllTextAsync(documentationPath, cancellationToken);
					ExampleDocumentationAssertions.VerifyProjectDocumentation(relativeProjectPath, documentation);
				}
				catch (Exception exception)
				{
					throw new InvalidOperationException($"Failed while generating documentation for example project '{relativeProjectPath}'.", exception);
				}
			}

			foreach (var configurationPath in standaloneConfigurationPaths)
			{
				var relativeConfigurationPath = Path.GetRelativePath(context.ExamplesRoot, configurationPath);
				configurationPaths.Add(configurationPath);

				var documentationPath = Path.Combine(standaloneDocumentationDirectory, Path.ChangeExtension(context.SanitizePath(relativeConfigurationPath), ".md"));
				await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
				{
					InputKind = ApplicationInputKind.ConfigurationFile,
					InputPaths = [configurationPath],
					OutputPath = documentationPath,
					IncludeDocumentationInput = true,
					Force = true
				}, cancellationToken);

				var documentation = await File.ReadAllTextAsync(documentationPath, cancellationToken);
				ExampleDocumentationAssertions.VerifyStandaloneConfigurationDocumentation(relativeConfigurationPath, documentation);
			}

			configurationPaths.Should().HaveCount(projectPaths.Length + standaloneConfigurationPaths.Length);
			Directory.GetFiles(projectDocumentationDirectory, "*.md").Should().HaveCount(projectPaths.Length);
			Directory.GetFiles(standaloneDocumentationDirectory, "*.md").Should().HaveCount(standaloneConfigurationPaths.Length);

			var healthExamplePath = Path.Combine(context.ExamplesRoot, "Features", "Example.ArchitectureHealth", "Example.ArchitectureHealth.csproj");
			var healthReportPath = Path.Combine(tempDirectory, "architecture-health.md");
			var healthResult = await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [healthExamplePath],
				OutputPath = healthReportPath,
				Force = true
			}, cancellationToken);
			healthResult.HasFindings.Should().BeTrue();

			var healthReport = await File.ReadAllTextAsync(healthReportPath, cancellationToken);
			ExampleDocumentationAssertions.VerifyHealthReport(healthReport);

			var mergedConfigurationPath = Path.Combine(tempDirectory, "AllExamples.Architecture.anl");
			await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.MergeConfig)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = configurationPaths,
				OutputPath = mergedConfigurationPath,
				Force = true
			}, cancellationToken);

			var mergedConfiguration = await File.ReadAllTextAsync(mergedConfigurationPath, cancellationToken);
			ExampleDocumentationAssertions.VerifyMergedConfiguration(mergedConfiguration);

			var mergedDocumentationPath = Path.Combine(tempDirectory, "AllExamples.architecture-documentation.md");
			await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [mergedConfigurationPath],
				OutputPath = mergedDocumentationPath,
				IncludeDocumentationInput = true,
				Force = true
			}, cancellationToken);

			var mergedDocumentation = await File.ReadAllTextAsync(mergedDocumentationPath, cancellationToken);
			ExampleDocumentationAssertions.VerifyMergedDocumentation(mergedDocumentation);
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
	public async Task ApplicationRunner_GeneratesDocumentationForSupportedConfigurationFeatures()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-documentation-feature-matrix-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var outputPath = Path.Combine(tempDirectory, "docs", "architecture-documentation.md");
			await File.WriteAllTextAsync(configPath, DocumentationFeatureMatrixFixture.Configuration, cancellationToken);

			var runner = new ApplicationRunner();
			await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputPath,
				IncludeDocumentationInput = true,
				Force = true
			}, cancellationToken);

			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			ExampleDocumentationAssertions.VerifyFeatureMatrixDocumentation(documentation);
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
	public async Task ApplicationRunner_GeneratesDocumentation_ForPackageReferenceBoundaryDataExample()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var context = ExampleRepositoryContext.Discover();
		var projectPath = context.GetExampleProjectPath("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Data");
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-package-reference-docs-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			using var host = new ExampleProjectAnalysisHost();
			var analysis = await host.AnalyzeProjectAsync(projectPath, cancellationToken);
			analysis.HasConfiguration.Should().BeTrue(
				"the direct project analysis host should load the package-reference example configuration before the documentation runner uses it. Diagnostics:{0}{1}",
				Environment.NewLine,
				string.Join(Environment.NewLine, analysis.AnalyzerDiagnosticMessages));

			var runner = new ApplicationRunner();
			var documentationPath = Path.Combine(tempDirectory, "package-reference-boundaries-data.md");
			await runner.ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = documentationPath,
				IncludeDocumentationInput = true,
				Force = true
			}, cancellationToken);

			File.Exists(documentationPath).Should().BeTrue();
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
