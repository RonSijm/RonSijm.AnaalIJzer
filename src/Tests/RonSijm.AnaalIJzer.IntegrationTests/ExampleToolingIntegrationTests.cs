using AwesomeAssertions;
using RonSijm.AnaalIJzer.Tooling;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests;

public sealed class ExampleToolingIntegrationTests
{
	[Fact]
	public async Task ToolRunner_MergesEveryExampleConfigurationAndGeneratesExtensiveDocumentation()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var repositoryRoot = FindRepositoryRoot();
		var examplesRoot = Path.Combine(repositoryRoot, "Examples");
		var projectPaths = Directory.EnumerateFiles(examplesRoot, "*.csproj", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
		var standaloneConfigurationPaths = FindStandaloneConfigurationExamples(examplesRoot);
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-all-examples-tooling-{Guid.NewGuid():N}");
		var generatedFileSnapshots = SnapshotGeneratedExampleFiles(repositoryRoot);
		Directory.CreateDirectory(tempDirectory);

		try
		{
			projectPaths.Should().NotBeEmpty("the repository should keep runnable example projects under Examples");

			var runner = new ToolRunner();
			var configurationPaths = new List<string>();
			var projectDocumentationDirectory = Path.Combine(tempDirectory, "ProjectDocumentation");
			var standaloneDocumentationDirectory = Path.Combine(tempDirectory, "StandaloneConfigurationDocumentation");
			Directory.CreateDirectory(projectDocumentationDirectory);
			Directory.CreateDirectory(standaloneDocumentationDirectory);

			foreach (var projectPath in projectPaths)
			{
				var relativeProjectPath = Path.GetRelativePath(examplesRoot, projectPath);
				var configurationPath = await GetXmlConfigurationForMergeAsync(runner, projectPath, relativeProjectPath, tempDirectory, cancellationToken);
				configurationPaths.Add(configurationPath);

				var documentationPath = Path.Combine(projectDocumentationDirectory, Path.ChangeExtension(SanitizePath(relativeProjectPath), ".md"));
				await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
				{
					InputKind = ToolInputKind.Project,
					InputPaths = [projectPath],
					OutputPath = documentationPath,
					IncludeCodeEvidence = true,
					IncludeDocumentationInput = true,
					Force = true
				}, cancellationToken);

				var documentation = await File.ReadAllTextAsync(documentationPath, cancellationToken);
				documentation.Should().Contain("## Code Evidence", $"project documentation for {relativeProjectPath} should include the richest project-backed evidence");
				documentation.Should().Contain("## Input Configuration", $"project documentation for {relativeProjectPath} should show the exact config it was based on");
				documentation.Should().Contain("```mermaid", $"project documentation for {relativeProjectPath} should include diagrams");
			}

			foreach (var configurationPath in standaloneConfigurationPaths)
			{
				var relativeConfigurationPath = Path.GetRelativePath(examplesRoot, configurationPath);
				configurationPaths.Add(configurationPath);

				var documentationPath = Path.Combine(standaloneDocumentationDirectory, Path.ChangeExtension(SanitizePath(relativeConfigurationPath), ".md"));
				await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
				{
					InputKind = ToolInputKind.ConfigurationFile,
					InputPaths = [configurationPath],
					OutputPath = documentationPath,
					IncludeDocumentationInput = true,
					Force = true
				}, cancellationToken);

				var documentation = await File.ReadAllTextAsync(documentationPath, cancellationToken);
				documentation.Should().Contain("## Input Configuration", $"configuration documentation for {relativeConfigurationPath} should show the exact XML it was based on");
				documentation.Should().Contain("```mermaid", $"configuration documentation for {relativeConfigurationPath} should include diagrams");
				documentation.Should().NotContain("## Code Evidence", $"standalone configuration documentation for {relativeConfigurationPath} cannot include compiled project evidence");
			}

			configurationPaths.Should().HaveCount(projectPaths.Length + standaloneConfigurationPaths.Length);
			Directory.GetFiles(projectDocumentationDirectory, "*.md").Should().HaveCount(projectPaths.Length);
			Directory.GetFiles(standaloneDocumentationDirectory, "*.md").Should().HaveCount(standaloneConfigurationPaths.Length);

			var healthExamplePath = Path.Combine(examplesRoot, "Features", "Example.ArchitectureHealth", "Example.ArchitectureHealth.csproj");
			var healthReportPath = Path.Combine(tempDirectory, "architecture-health.md");
			var healthResult = await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.Inspect)
			{
				InputKind = ToolInputKind.Project,
				InputPaths = [healthExamplePath],
				OutputPath = healthReportPath,
				Force = true
			}, cancellationToken);
			healthResult.HasFindings.Should().BeTrue();
			var healthReport = await File.ReadAllTextAsync(healthReportPath, cancellationToken);
			healthReport.Should().Contain("Configured cycle");
			healthReport.Should().Contain("Observed dependency cycle");
			healthReport.Should().Contain("Unclassified type");
			healthReport.Should().Contain("Unmatched matcher");
			healthReport.Should().Contain("Stale exception");
			healthReport.Should().Contain("Unused allowed edge");

			var mergedConfigurationPath = Path.Combine(tempDirectory, "AllExamples.ArchitecturalLevels.xml");
			await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.MergeConfig)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = configurationPaths,
				OutputPath = mergedConfigurationPath,
				Force = true
			}, cancellationToken);

			var mergedConfiguration = await File.ReadAllTextAsync(mergedConfigurationPath, cancellationToken);
			mergedConfiguration.Should().Contain("<Layer ");
			mergedConfiguration.Should().Contain("<AllowedDependency ");
			mergedConfiguration.Should().Contain("<BlockedDependency ");
			mergedConfiguration.Should().NotContain("<Include ");

			var mergedDocumentationPath = Path.Combine(tempDirectory, "AllExamples.architecture-documentation.md");
			await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [mergedConfigurationPath],
				OutputPath = mergedDocumentationPath,
				IncludeDocumentationInput = true,
				Force = true
			}, cancellationToken);

			var mergedDocumentation = await File.ReadAllTextAsync(mergedDocumentationPath, cancellationToken);
			mergedDocumentation.Should().Contain("## Dependency Flow");
			mergedDocumentation.Should().Contain("```mermaid");
			mergedDocumentation.Should().Contain("## Rules In Configuration Order");
			mergedDocumentation.Should().Contain("## Input Configuration");
			mergedDocumentation.Should().Contain("allowedSites", "site-filter labels should survive the all-examples merge and appear in documentation");
			mergedDocumentation.Should().Contain("QuerySurface", "scenario-specific layers should survive the all-examples merge");
			mergedDocumentation.Should().Contain("Persistence", "starter configuration layers should survive the all-examples merge");
			mergedDocumentation.Should().Contain("## Type Policies", "allowed and forbidden type-policy examples should survive the all-examples merge");
			mergedDocumentation.Should().Contain("| Allowed | `global` |", "global allow-list examples should be rendered with their scope");
			mergedDocumentation.Should().Contain("| Forbidden | `Query` |", "layer-scoped forbidden examples should be rendered with their scope");
		}
		finally
		{
			RestoreGeneratedExampleFiles(generatedFileSnapshots);
			Directory.Delete(tempDirectory, recursive: true);
		}
	}

	private static async Task<string> GetXmlConfigurationForMergeAsync(ToolRunner runner, string projectPath, string relativeProjectPath, string tempDirectory, CancellationToken cancellationToken)
	{
		var projectDirectory = Path.GetDirectoryName(projectPath)!;
		var fileConfigurationPath = Path.Combine(projectDirectory, "ArchitecturalLevels.xml");
		if (File.Exists(fileConfigurationPath))
		{
			return fileConfigurationPath;
		}

		var exportDirectory = Path.Combine(tempDirectory, "ExportedInlineSettings");
		Directory.CreateDirectory(exportDirectory);
		var exportedConfigurationPath = Path.Combine(exportDirectory, Path.ChangeExtension(SanitizePath(relativeProjectPath), ".xml"));
		await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.ExportConfig)
		{
			InputKind = ToolInputKind.Project,
			InputPaths = [projectPath],
			OutputPath = exportedConfigurationPath,
			Force = true
		}, cancellationToken);

		File.Exists(exportedConfigurationPath).Should().BeTrue($"inline settings from {relativeProjectPath} should be exported before merging");
		return exportedConfigurationPath;
	}

	private static Dictionary<string, byte[]?> SnapshotGeneratedExampleFiles(string repositoryRoot)
	{
		var paths = new[]
		{
			Path.Combine(repositoryRoot, "Examples", "Documentation", "Generated", "architectural-violations.md"),
			Path.Combine(repositoryRoot, "Examples", "Documentation", "Generated", "architecture-documentation.md")
		};

		return paths.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null, StringComparer.OrdinalIgnoreCase);
	}

	private static void RestoreGeneratedExampleFiles(IReadOnlyDictionary<string, byte[]?> snapshots)
	{
		foreach (var (path, contents) in snapshots)
		{
			if (contents is null)
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}

				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllBytes(path, contents);
		}
	}

	private static string FindRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "RonSijm.AnaalIJzer.WithExamples.slnx")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Could not find repository root containing RonSijm.AnaalIJzer.WithExamples.slnx.");
	}

	private static string[] FindStandaloneConfigurationExamples(string examplesRoot)
	{
		var starterConfigDirectory = Path.Combine(examplesRoot, "StarterConfigs");
		return Directory.Exists(starterConfigDirectory)
			? Directory.EnumerateFiles(starterConfigDirectory, "*.xml", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
			: [];
	}

	private static string SanitizePath(string path)
	{
		var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
		var characters = path.Select(character => invalidCharacters.Contains(character) || character is '\\' or '/' or ':' ? '-' : character).ToArray();
		return new string(characters).Trim('-');
	}
}
