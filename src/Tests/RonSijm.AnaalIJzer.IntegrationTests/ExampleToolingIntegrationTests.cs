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
				if (relativeProjectPath.Contains("Example.CombinedMatchers", StringComparison.Ordinal))
				{
					documentation.Should().Contain("Class endsWith=\"Repository\" typeKind=\"Interface\"");
					documentation.Should().Contain("IExampleRepository");
					documentation.Should().Contain("ExampleRepository");
				}
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

			var mergedConfigurationPath = Path.Combine(tempDirectory, "AllExamples.Architecture.anl");
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
			mergedConfiguration.Should().Contain("typeKind=\"Interface\"");
			mergedConfiguration.Should().Contain("appliesToDescendants=\"true\"");
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
			mergedDocumentation.Should().Contain("appliesToDescendants=\"true\"", "cascading dependency rules should survive merge and documentation generation");
			mergedDocumentation.Should().Contain("applies to descendants", "cascading dependency rules should be visible in rendered documentation");
			mergedDocumentation.Should().Contain("typeKind=\"Interface\"", "combined matcher conditions should survive merge and documentation generation");
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

	[Fact]
	public async Task ToolRunner_GeneratesDocumentationForSupportedConfigurationFeatures()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-documentation-feature-matrix-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var outputPath = Path.Combine(tempDirectory, "docs", "architecture-documentation.md");
			await File.WriteAllTextAsync(configPath, DocumentationFeatureMatrixConfiguration, cancellationToken);

			var runner = new ToolRunner();
			await runner.ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputPath,
				IncludeDocumentationInput = true,
				Force = true
			}, cancellationToken);

			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("# Architecture Documentation");
			documentation.Should().Contain("Feature matrix configuration for documentation coverage.");
			documentation.Should().Contain("## Dependency Flow");
			documentation.Should().Contain("```mermaid");
			documentation.Should().Contain("subgraph SG_Ordering");
			documentation.Should().Contain("Ordering/Application");
			documentation.Should().Contain("Ordering/Repository");
			documentation.Should().Contain("### Universal Rules");
			documentation.Should().Contain("all layers");
			documentation.Should().Contain("allowed sites: Constructor, Method");
			documentation.Should().Contain("allowed sites: MethodReturn");
			documentation.Should().Contain("blocked sites: Field; applies to descendants");
			documentation.Should().Contain("appliesToDescendants=\"true\"");
			documentation.Should().Contain("## Type Policies");
			documentation.Should().Contain("| Allowed | `global` | `Class startsWith=\"I\" endsWith=\"Contract\" typeKind=\"Interface\"` | Interface contracts are globally approved. |");
			documentation.Should().Contain("| Allowed | `Ordering/Application` | `Class endsWith=\"Contract\" typeKind=\"Interface\"` | Application code may consume contract interfaces. |");
			documentation.Should().Contain("| Forbidden | `global` | `Class endsWith=\"Store\" typeKind=\"Class\"` | Use Repository instead. |");
			documentation.Should().Contain("| Forbidden | `Ordering/Repository` | `Namespace contains=\".Legacy\"` | Legacy persistence namespace is blocked. |");
			documentation.Should().Contain("## Rules In Configuration Order");
			documentation.Should().Contain("- **Layer** `Ordering`");
			documentation.Should().Contain("Ordering boundary with nested application and repository roles.");
			documentation.Should().Contain("- **Assembly** `Assembly exactName=\"CandyShop.Persistence\"`");
			documentation.Should().Contain("- **Exceptions** `Exceptions`");
			documentation.Should().Contain("Legacy store names are grandfathered.");
			documentation.Should().Contain("- **Fix** `Fix Repository`");
			documentation.Should().Contain("Rename=\"Repository\"");
			documentation.Should().Contain("## Input Configuration");
			documentation.Should().Contain("This documentation was generated from the following architecture configuration: `Architecture.anl`.");
			documentation.Should().Contain("enableDocumentation=\"true\"");
			documentation.Should().Contain("documentationPath=\"docs\\architecture-documentation.md\"");
			documentation.Should().Contain("enableReport=\"true\"");
			documentation.Should().Contain("requireRecognizedDependencies=\"Constructor, Local\"");
			documentation.Should().Contain("requireRecognizedDependencies=\"MethodReturn\"");
			documentation.Should().Contain("enforceAcyclic=\"true\"");
		}
		finally
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
	}

	private const string DocumentationFeatureMatrixConfiguration = """
<ArchitecturalLevels enableDocumentation="true"
                     documentationPath="docs\architecture-documentation.md"
                     enableReport="true"
                     reportPath="docs\architectural-violations.md"
                     requireRecognizedDependencies="Constructor, Local"
                     enforceAcyclic="true"
                     description="Feature matrix configuration for documentation coverage.">
  <Allowed description="Global approved dependency type names.">
    <Class startsWith="I"
           endsWith="Contract"
           typeKind="Interface"
           description="Interface contracts are globally approved." />
  </Allowed>
  <Forbidden description="Global forbidden naming policies.">
    <Class endsWith="Store"
           typeKind="Class"
           comment="Use Repository instead."
           description="Store suffixes are legacy names.">
      <Fix Rename="Repository"
           description="Offer the repository suffix as a rename target." />
      <Exceptions description="Legacy store names are grandfathered.">
        <Class typeName="LegacyStore"
               description="Legacy store exception." />
      </Exceptions>
    </Class>
  </Forbidden>
  <Layer name="Ordering"
         requireRecognizedDependencies="MethodReturn"
         description="Ordering boundary with nested application and repository roles.">
    <Namespace startsWith="CandyShop.Ordering"
               description="Ordering namespace scope." />
    <Layer name="Application"
           description="Ordering application services.">
      <Class endsWith="Service"
             typeKind="Class"
             description="Service classes in the ordering boundary." />
      <Allowed description="Application dependency allow-list.">
        <Class endsWith="Contract"
               typeKind="Interface"
               description="Application code may consume contract interfaces." />
      </Allowed>
    </Layer>
    <Layer name="Repository"
           description="Ordering persistence implementation.">
      <Assembly exactName="CandyShop.Persistence"
                description="Repository implementation assembly." />
      <Class endsWith="Repository"
             typeKind="Class"
             description="Repository implementation classes." />
      <Forbidden description="Repository scoped forbidden types.">
        <Namespace contains=".Legacy"
                   description="Legacy persistence namespace is blocked." />
      </Forbidden>
    </Layer>
    <AllowedDependency from="Application"
                       to="Repository"
                       allowedSites="Constructor, Method"
                       description="Services may depend on repositories through constructor and method sites." />
    <BlockedDependency from="Application"
                       to="Repository"
                       allowedSites="MethodReturn"
                       description="Services may not expose repositories as method return values." />
  </Layer>
  <Layer name="Shared"
         description="Shared contracts used by other boundaries.">
    <Class endsWith="Contract"
           typeKind="Interface"
           description="Shared contract interfaces." />
  </Layer>
  <AllowedDependency from="*"
                     to="Shared"
                     blockedSites="Field"
                     appliesToDescendants="true"
                     description="Any layer may use shared contracts except as stored fields." />
  <AllowedDependency from="Ordering"
                     to="Shared"
                     allowedSites="Constructor, MethodReturn"
                     description="Ordering may depend on shared contracts and return them." />
</ArchitecturalLevels>
""";

	private static async Task<string> GetXmlConfigurationForMergeAsync(ToolRunner runner, string projectPath, string relativeProjectPath, string tempDirectory, CancellationToken cancellationToken)
	{
		var projectDirectory = Path.GetDirectoryName(projectPath)!;
		var fileConfigurationPath = Path.Combine(projectDirectory, "Architecture.anl");
		if (File.Exists(fileConfigurationPath))
		{
			return fileConfigurationPath;
		}

		var exportDirectory = Path.Combine(tempDirectory, "ExportedInlineSettings");
		Directory.CreateDirectory(exportDirectory);
		var exportedConfigurationPath = Path.Combine(exportDirectory, Path.ChangeExtension(SanitizePath(relativeProjectPath), ".anl"));
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
			? Directory.EnumerateFiles(starterConfigDirectory, "*.anl", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
			: [];
	}

	private static string SanitizePath(string path)
	{
		var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
		var characters = path.Select(character => invalidCharacters.Contains(character) || character is '\\' or '/' or ':' ? '-' : character).ToArray();
		return new string(characters).Trim('-');
	}
}
