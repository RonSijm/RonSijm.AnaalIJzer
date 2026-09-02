using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.IntegrationTests.Support;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests;

public sealed class ExamplesIntegrationTests
{
	[Fact]
	public async Task ExampleProjects_BuildWithExpectedAnalyzerDiagnostics()
	{
		var context = ExampleRepositoryContext.Discover();
		var failures = new List<string>();
		ExampleSettingsValidation.ValidateExampleSettingsConfigs(context, failures);

		using var generatedFiles = new GeneratedExampleFilesScope(context);
		using var host = new ExampleProjectAnalysisHost();

		foreach (var expectation in ExampleBuildExpectationCatalog.All)
		{
			await ValidateExampleProjectAsync(context, host, expectation, failures);
		}

		failures.Should().BeEmpty("all example projects should produce their documented analyzer diagnostics:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine + Environment.NewLine, failures));
	}

	[Fact]
	public void ExamplesDirectoryBuildProps_EnableExamplesByDefaultAndAttachEngineAnalyzer()
	{
		var context = ExampleRepositoryContext.Discover();
		var propsPath = Path.Combine(context.ExamplesRoot, "Directory.Build.props");
		var document = System.Xml.Linq.XDocument.Load(propsPath);
		var projectReferences = document
			.Descendants()
			.Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
			.Select(element => element.Attribute("Include")?.Value)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToArray();
		var analyzerBuildTargets = document
			.Descendants()
			.Where(element => string.Equals(element.Name.LocalName, "MSBuild", StringComparison.Ordinal))
			.Select(element => element.Attribute("Projects")?.Value)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToArray();
		var enableAnalyzerOnDebug = document
			.Descendants()
			.FirstOrDefault(element => string.Equals(element.Name.LocalName, "EnableAnalyzerOnDebug", StringComparison.Ordinal))
			?.Value
			?.Trim();
		var additionalFiles = document
			.Descendants()
			.Where(element => string.Equals(element.Name.LocalName, "AdditionalFiles", StringComparison.Ordinal))
			.Select(element => element.Attribute("Include")?.Value)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.ToArray();

		enableAnalyzerOnDebug.Should().Be("true", "examples should show their analyzer behavior in ordinary Debug IDE builds by default");
		projectReferences.Should().BeEmpty("example projects should not inherit analyzer implementation projects as visible project dependencies");
		analyzerBuildTargets.Should().Contain("$(AnaalIJzerEngineProjectPath)",
			"the shared example props should build the Engine analyzer entry point via the centralized path property before attaching its analyzer DLLs");
		additionalFiles.Should().Contain("$(MSBuildProjectDirectory)\\**\\*.anl",
			"example projects should be able to keep drop-in rule packs in project-local subfolders");
	}

	[Fact]
	public async Task InlineExampleProjects_ProvideEditorLayerSnapshots()
	{
		var context = ExampleRepositoryContext.Discover();
		var projectPath = context.GetExampleProjectPath("Diagnostics/Example.Arch001.NoEdge");

		using var host = new ExampleProjectAnalysisHost();
		var snapshot = await host.CreateEditorSnapshotAsync(projectPath, "Example.cs", TestContext.Current.CancellationToken);

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.HasConfigurationIssues.Should().BeFalse();
		snapshot.UnclassifiedTypeIndicators.Should().BeEmpty();
		snapshot.GraphSnapshot.ConfigurationSource.Kind.Should().Be(ArchitectureConfigurationSourceKind.InlineAssemblyMetadata);
		snapshot.GraphSnapshot.ConfigurationSource.Path.Should().EndWith(Path.Combine("Diagnostics", "Example.Arch001.NoEdge", "Example.cs"));
		snapshot.LayerIndicators.Should().Contain(indicator => indicator.TypeName == "HungryCustomer" && indicator.LayerPath == "Customer");
		snapshot.LayerIndicators.Should().Contain(indicator => indicator.TypeName == "TableWaiter" && indicator.LayerPath == "Waiter");
		snapshot.LayerIndicators.Should().Contain(indicator => indicator.TypeName == "IIngredientPantry" && indicator.LayerPath == "Pantry");
	}

	[Fact]
	public async Task VisualStudioSiteDiagnosticsExample_ProvidesEverySupportedSite()
	{
		var context = ExampleRepositoryContext.Discover();
		var projectPath = context.GetExampleProjectPath("Documentation/Example.VisualStudioSiteDiagnostics");

		using var host = new ExampleProjectAnalysisHost();
		var snapshot = await host.CreateEditorSnapshotAsync(projectPath, "All_Site_Diagnostics_Showcase.cs", TestContext.Current.CancellationToken);
		var actualSites = snapshot.SiteIndicators
			.Where(indicator => indicator.CallerTypeName == "AllSiteDiagnosticsShowcase")
			.Select(indicator => indicator.Site)
			.ToHashSet(StringComparer.Ordinal);
		var missingSites = ArchitectureDependencySites.All
			.Where(site => !actualSites.Contains(site))
			.ToArray();

		snapshot.HasConfiguration.Should().BeTrue();
		snapshot.HasConfigurationIssues.Should().BeFalse();
		missingSites.Should().BeEmpty("the Visual Studio showcase should provide every site controlled by Layer Information and Site Diagnostics settings");
	}

	[Fact]
	public async Task StructuralDeclarationMatchersExample_BuildsWithExpectedDiagnostic()
	{
		var context = ExampleRepositoryContext.Discover();
		var expectation = ExampleBuildExpectationCatalog.All.Single(item => string.Equals(item.RelativeProjectPath, "Features/Example.StructuralDeclarationMatchers", StringComparison.Ordinal));
		var projectPath = context.GetExampleProjectPath(expectation.RelativeProjectPath);

		using var host = new ExampleProjectAnalysisHost();
		var result = await host.AnalyzeProjectAsync(projectPath, TestContext.Current.CancellationToken);

		result.WorkspaceFailures.Should().BeEmpty();
		result.HasConfiguration.Should().BeTrue();
		result.CompilerErrors.Should().BeEmpty();
		result.AnalyzerDiagnostics.Should().BeEquivalentTo(expectation.Diagnostics);
		result.AnalyzerDiagnosticMessages.Should().ContainSingle(message => message.Contains("CreatePizzaRequest", StringComparison.Ordinal));
	}

	[Fact]
	public void ExampleProjects_AreRegisteredAndDocumented()
	{
		var context = ExampleRepositoryContext.Discover();
		var expectedPaths = ExampleBuildExpectationCatalog.All.Select(expectation => expectation.RelativeProjectPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var actualPaths = context.FindAllExampleProjectPaths()
			.Select(projectPath => Path.GetRelativePath(context.ExamplesRoot, Path.GetDirectoryName(projectPath)!).Replace('\\', '/'))
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		actualPaths.Should().BeEquivalentTo(expectedPaths, "every example project should be build-verified by the integration test");

		var documentation = string.Join(Environment.NewLine, Directory.EnumerateFiles(Path.Combine(context.RepositoryRoot, "docs"), "*.md", SearchOption.AllDirectories)
			.Concat([Path.Combine(context.ExamplesRoot, "README.md")])
			.Select(File.ReadAllText));
		var undocumented = ExampleBuildExpectationCatalog.All
			.Select(expectation => Path.GetFileName(expectation.RelativeProjectPath))
			.Where(projectName => !documentation.Contains(projectName, StringComparison.Ordinal))
			.ToArray();

		undocumented.Should().BeEmpty("every build-verified example should be discoverable from docs or the examples index");
	}

	[Fact]
	public void ExampleProjectFiles_KeepDirectDependenciesMinimal()
	{
		var context = ExampleRepositoryContext.Discover();
		var expectedProjectReferences = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Application"] =
			[
				@"..\Example.ProjectReferenceBoundaries.Domain\Example.ProjectReferenceBoundaries.Domain.csproj"
			],
			["Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Domain"] =
			[
				@"..\Example.ProjectReferenceBoundaries.Infrastructure\Example.ProjectReferenceBoundaries.Infrastructure.csproj"
			]
		};
		var expectedPackageReferences = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Data"] =
			[
				"Microsoft.Extensions.Logging"
			],
			["Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Domain"] =
			[
				"Microsoft.Extensions.Logging"
			]
		};

		foreach (var projectPath in context.FindAllExampleProjectPaths())
		{
			var relativeProjectDirectory = Path.GetRelativePath(context.ExamplesRoot, Path.GetDirectoryName(projectPath)!).Replace('\\', '/');
			var document = System.Xml.Linq.XDocument.Load(projectPath);
			var projectReferences = document
				.Descendants()
				.Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
				.Select(element => element.Attribute("Include")?.Value)
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.ToArray();
			var packageReferences = document
				.Descendants()
				.Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal))
				.Select(element => element.Attribute("Include")?.Value)
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.ToArray();
			var analyzerReferences = document
				.Descendants()
				.Where(element => string.Equals(element.Name.LocalName, "Analyzer", StringComparison.Ordinal))
				.Select(element => element.Attribute("Include")?.Value)
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.ToArray();
			var assemblyReferences = document
				.Descendants()
				.Where(element => string.Equals(element.Name.LocalName, "Reference", StringComparison.Ordinal))
				.Select(element => element.Attribute("Include")?.Value)
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.ToArray();

			projectReferences.Should().BeEquivalentTo(
				expectedProjectReferences.TryGetValue(relativeProjectDirectory, out var expectedProjects) ? expectedProjects : [],
				$"{relativeProjectDirectory} should only declare direct project references when the scenario itself is demonstrating project-boundary rules");
			packageReferences.Should().BeEquivalentTo(
				expectedPackageReferences.TryGetValue(relativeProjectDirectory, out var expectedPackages) ? expectedPackages : [],
				$"{relativeProjectDirectory} should only declare direct package references when the scenario itself is demonstrating package-boundary rules");
			analyzerReferences.Should().BeEmpty($"{relativeProjectDirectory} should not hard-code analyzer DLL references in its own project file");
			assemblyReferences.Should().BeEmpty($"{relativeProjectDirectory} should not carry extra assembly references beyond the SDK defaults");
		}
	}

	private static async Task ValidateExampleProjectAsync(ExampleRepositoryContext context, ExampleProjectAnalysisHost host, ExampleBuildExpectation expectation, List<string> failures)
	{
		var projectPath = context.GetExampleProjectPath(expectation.RelativeProjectPath);
		if (!File.Exists(projectPath))
		{
			failures.Add($"{expectation.RelativeProjectPath}: missing project file at {projectPath}");

			return;
		}

		var projectDirectory = Path.GetDirectoryName(projectPath)!;
		var fileConfigPath = Path.Combine(projectDirectory, "Architecture.anl");
		var inlineSettingsPath = Path.Combine(projectDirectory, "Properties", "AnaalIJzerSettings.cs");
		if (File.Exists(inlineSettingsPath))
		{
			failures.Add($"{expectation.RelativeProjectPath}: inline settings should live in the example source file for simple examples, or Architecture.anl for broader examples; remove {inlineSettingsPath}.");
		}

		var oldInlineSettingsPath = Path.Combine(projectDirectory, "ArchitecturalLevels.cs");
		if (File.Exists(oldInlineSettingsPath))
		{
			failures.Add($"{expectation.RelativeProjectPath}: inline settings should live in the example source file, not {oldInlineSettingsPath}.");
		}

		var result = await host.AnalyzeProjectAsync(projectPath, TestContext.Current.CancellationToken);
		if (result.WorkspaceFailures.Length > 0)
		{
			failures.Add($"{expectation.RelativeProjectPath}: workspace load failures:{Environment.NewLine}{string.Join(Environment.NewLine, result.WorkspaceFailures)}");
		}

		if (!result.HasConfiguration)
		{
			failures.Add($"{expectation.RelativeProjectPath}: no architectural configuration rules were loaded for the project. Analyzer diagnostics:{Environment.NewLine}{string.Join(Environment.NewLine, result.AnalyzerDiagnosticMessages)}");
		}

		if (result.CompilerErrors.Length > 0)
		{
			failures.Add($"{expectation.RelativeProjectPath}: unexpected compiler errors:{Environment.NewLine}{string.Join(Environment.NewLine, result.CompilerErrors)}");
		}

		if (!DictionariesEqual(result.AnalyzerDiagnostics, expectation.Diagnostics))
		{
			failures.Add($"{expectation.RelativeProjectPath}: expected diagnostics {FormatDiagnostics(expectation.Diagnostics)}, got {FormatDiagnostics(result.AnalyzerDiagnostics)}.{Environment.NewLine}{string.Join(Environment.NewLine, result.AnalyzerDiagnosticMessages)}");
		}

		if (expectation.ConfigStyle == ExampleConfigStyle.InlineInExample)
		{
			if (File.Exists(fileConfigPath))
			{
				failures.Add($"{expectation.RelativeProjectPath}: simple one-file examples should keep settings inline in the example source file; remove {fileConfigPath}.");
			}

			var inlineSourceFiles = ExampleSettingsValidation.FindInlineSettingsSourceFiles(projectDirectory);
			if (inlineSourceFiles.Length == 0)
			{
				failures.Add($"{expectation.RelativeProjectPath}: missing AssemblyMetadata(\"AnaalIJzerSettings\", ...) in an example source file.");
			}
			else if (inlineSourceFiles.Length > 1)
			{
				failures.Add($"{expectation.RelativeProjectPath}: simple inline examples should keep exactly one AssemblyMetadata(\"AnaalIJzerSettings\", ...) source file, found {inlineSourceFiles.Length}.");
			}

			if (string.IsNullOrWhiteSpace(result.InlineConfigXml))
			{
				failures.Add($"{expectation.RelativeProjectPath}: missing AssemblyMetadata(\"AnaalIJzerSettings\", ...) inline settings.");
			}
			else
			{
				ExampleSettingsValidation.ValidateInlineConfigXml($"{expectation.RelativeProjectPath}: AnaalIJzerSettings", result.InlineConfigXml, context.SchemaPath, failures);
			}

			return;
		}

		if (!File.Exists(fileConfigPath))
		{
			failures.Add($"{expectation.RelativeProjectPath}: broader examples should use Architecture.anl.");
		}
	}

	private static bool DictionariesEqual(IReadOnlyDictionary<string, int> left, IReadOnlyDictionary<string, int> right)
	{
		var result = left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var count) && count == pair.Value);

		return result;
	}

	private static string FormatDiagnostics(IReadOnlyDictionary<string, int> diagnostics)
	{
		var result = diagnostics.Count == 0 ? "<none>" : string.Join(", ", diagnostics.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"));

		return result;
	}
}
