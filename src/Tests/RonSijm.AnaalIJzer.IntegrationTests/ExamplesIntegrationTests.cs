using AwesomeAssertions;
using RonSijm.AnaalIJzer.Diagnostics;
using Xunit;

namespace RonSijm.AnaalIJzer.IntegrationTests;

public sealed class ExamplesIntegrationTests
{
	private static readonly ExampleBuildExpectation[] Expectations =
	[
		Expect("Diagnostics/Example.Arch001.GenericTypeArgument", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 3)),
		Expect("Diagnostics/Example.Arch001.NoEdge", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		ExpectFile("Diagnostics/Example.Arch001.NonConstructorInjection", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 8)),
		Expect("Diagnostics/Example.Arch001.SkipsLayer", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		Expect("Diagnostics/Example.Arch002.UnrecognizedDependency", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1)),
		Expect("Diagnostics/Example.Arch003.ForbiddenType", (ArchitecturalDiagnosticIds.ForbiddenDependency, 1)),
		Expect("Diagnostics/Example.Arch004.WrongDirection", (ArchitecturalDiagnosticIds.WrongDirectionDependency, 1)),
		Expect("Diagnostics/Example.Arch005.SameLayer", (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Diagnostics/Example.Arch006.UnknownLayer", (ArchitecturalDiagnosticIds.InvalidConfiguration, 1)),
		Expect("Diagnostics/Example.Arch007.CyclicGraph", (ArchitecturalDiagnosticIds.CyclicDependencyGraph, 1)),
		ExpectFile("Features/Example.AllowedSites", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 26)),
		Expect("Features/Example.AllowedTypes", (ArchitecturalDiagnosticIds.ForbiddenDependency, 1)),
		ExpectFile("Features/Example.ArchitectureHealth"),
		Expect("Features/Example.AssemblyMatcher", (ArchitecturalDiagnosticIds.WrongDirectionDependency, 1)),
		Expect("Features/Example.BlockedDependency", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		ExpectFile("Features/Example.CascadingDependencyRules"),
		Expect("Features/Example.CombinedMatchers", (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Features/Example.Exceptions", (ArchitecturalDiagnosticIds.ForbiddenDependency, 1)),
		ExpectFile("Features/Example.IncludeSettings", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		Expect("Features/Example.InlineXml", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1)),
		Expect("Features/Example.LayerScopedRecognizedDependencies", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1)),
		Expect("Features/Example.NestedExceptions", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 2)),
		ExpectFile("Features/Example.NestedLayers"),
		Expect("Features/Example.NonClassCallers", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 3)),
		ExpectFile("Features/Example.RequiredRecognizedDependencySites", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 13)),
		Expect("Features/Example.SameLayerInheritance", (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Features/Example.ScopedTypePolicies", (ArchitecturalDiagnosticIds.ForbiddenDependency, 2)),
		ExpectFile("Scenarios/Example.RepositoryQuerySurface", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 2)),
		ExpectFile("Documentation/Example.DocumentationDemo", (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1)),
		Expect("Documentation/Example.ReportDemo", (ArchitecturalDiagnosticIds.IllegalLevelDependency, 1), (ArchitecturalDiagnosticIds.UnrecognizedDependency, 1), (ArchitecturalDiagnosticIds.ForbiddenDependency, 1), (ArchitecturalDiagnosticIds.WrongDirectionDependency, 1), (ArchitecturalDiagnosticIds.SameLayerDependency, 1)),
		Expect("Features/Example.WildcardTo")
	];

	[Fact]
	public async Task ExampleProjects_BuildWithExpectedAnalyzerDiagnostics()
	{
		var repositoryRoot = FindRepositoryRoot();
		var schemaPath = Path.Combine(repositoryRoot, "src", "Main", "RonSijm.AnaalIJzer", "Scheme", "AnaalIJzer.xsd");
		var failures = new List<string>();
		ValidateExampleSettingsConfigs(repositoryRoot, schemaPath, failures);

		var generatedFileSnapshots = SnapshotGeneratedExampleFiles(repositoryRoot);
		using var host = new ExampleProjectAnalysisHost();

		try
		{
			foreach (var expectation in Expectations)
			{
				var projectName = Path.GetFileName(expectation.RelativeProjectPath);
				var projectPath = Path.Combine(repositoryRoot, "Examples", expectation.RelativeProjectPath, projectName + ".csproj");
				if (!File.Exists(projectPath))
				{
					failures.Add($"{expectation.RelativeProjectPath}: missing project file at {projectPath}");
					continue;
				}

				var projectDirectory = Path.GetDirectoryName(projectPath)!;
				var fileConfigPath = Path.Combine(projectDirectory, "Architecture.anl");
				var inlineSettingsPath = Path.Combine(projectDirectory, "Properties", "AnaalIJzerSettings.cs");
				if (File.Exists(inlineSettingsPath))
				{
					failures.Add($"{expectation.RelativeProjectPath}: inline settings should live in Example.cs for simple examples, or Architecture.anl for broader examples; remove {inlineSettingsPath}.");
				}

				var oldInlineSettingsPath = Path.Combine(projectDirectory, "ArchitecturalLevels.cs");
				if (File.Exists(oldInlineSettingsPath))
				{
					failures.Add($"{expectation.RelativeProjectPath}: inline settings should live in Example.cs, not {oldInlineSettingsPath}.");
				}

				var result = await host.AnalyzeProjectAsync(projectPath, TestContext.Current.CancellationToken);
				if (result.WorkspaceFailures.Length > 0)
				{
					failures.Add($"{expectation.RelativeProjectPath}: workspace load failures:{Environment.NewLine}{string.Join(Environment.NewLine, result.WorkspaceFailures)}");
				}

				if (result.CompilerErrors.Length > 0)
				{
					failures.Add($"{expectation.RelativeProjectPath}: unexpected compiler errors:{Environment.NewLine}{string.Join(Environment.NewLine, result.CompilerErrors)}");
				}

				if (!DictionariesEqual(result.AnalyzerDiagnostics, expectation.Diagnostics))
				{
					failures.Add($"{expectation.RelativeProjectPath}: expected diagnostics {FormatDiagnostics(expectation.Diagnostics)}, got {FormatDiagnostics(result.AnalyzerDiagnostics)}.");
				}

				if (expectation.ConfigStyle == ExampleConfigStyle.InlineInExample)
				{
					if (File.Exists(fileConfigPath))
					{
						failures.Add($"{expectation.RelativeProjectPath}: simple one-file examples should keep settings inline in Example.cs; remove {fileConfigPath}.");
					}

					var examplePath = Path.Combine(projectDirectory, "Example.cs");
					if (!File.ReadAllText(examplePath).Contains("AssemblyMetadata(\"AnaalIJzerSettings\"", StringComparison.Ordinal))
					{
						failures.Add($"{expectation.RelativeProjectPath}: missing AssemblyMetadata(\"AnaalIJzerSettings\", ...) in Example.cs.");
					}

					if (string.IsNullOrWhiteSpace(result.InlineConfigXml))
					{
						failures.Add($"{expectation.RelativeProjectPath}: missing AssemblyMetadata(\"AnaalIJzerSettings\", ...) inline settings.");
					}
					else
					{
						ValidateXmlContent($"{expectation.RelativeProjectPath}: AnaalIJzerSettings", result.InlineConfigXml, null, schemaPath, requireSchemaHint: false, failures);
					}
				}
				else
				{
					if (!File.Exists(fileConfigPath))
					{
						failures.Add($"{expectation.RelativeProjectPath}: broader examples should use Architecture.anl.");
					}
				}
			}
		}
		finally
		{
			RestoreGeneratedExampleFiles(generatedFileSnapshots);
		}

		failures.Should().BeEmpty("all example projects should produce their documented analyzer diagnostics:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine + Environment.NewLine, failures));
	}

	private static void ValidateExampleSettingsConfigs(string repositoryRoot, string schemaPath, List<string> failures)
	{
		var examplesPath = Path.Combine(repositoryRoot, "Examples");
		var settingsPaths = Directory
			.EnumerateFiles(examplesPath, "*.anl", SearchOption.AllDirectories)
			.Concat(Directory.EnumerateFiles(examplesPath, "*.xml", SearchOption.AllDirectories))
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

		foreach (var settingsPath in settingsPaths)
		{
			var content = File.ReadAllText(settingsPath);
			if (!content.Contains("<ArchitecturalLevels", StringComparison.Ordinal))
			{
				continue;
			}

			var relativePath = Path.GetRelativePath(repositoryRoot, settingsPath);
			ValidateXmlContent(relativePath, content, settingsPath, schemaPath, requireSchemaHint: true, failures);
		}
	}

	private static void ValidateXmlContent(string label, string content, string? xmlPath, string schemaPath, bool requireSchemaHint, List<string> failures)
	{
		var validationMessages = new List<string>();
		var settings = new System.Xml.XmlReaderSettings
		{
			ValidationType = System.Xml.ValidationType.Schema,
			ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings
		};

		settings.Schemas.Add(string.Empty, schemaPath);
		settings.ValidationEventHandler += (_, args) => validationMessages.Add($"{args.Severity}: {args.Message}");

		try
		{
			using var textReader = new StringReader(content);
			using var reader = System.Xml.XmlReader.Create(textReader, settings);
			reader.MoveToContent();
			var schemaLocation = reader.GetAttribute("noNamespaceSchemaLocation", System.Xml.Schema.XmlSchema.InstanceNamespace);
			if (requireSchemaHint && string.IsNullOrWhiteSpace(schemaLocation))
			{
				failures.Add($"{label}: missing xsi:noNamespaceSchemaLocation schema hint.");
			}
			else if (requireSchemaHint && xmlPath is not null && schemaLocation is not null && !SchemaHintExists(xmlPath, schemaLocation))
			{
				failures.Add($"{label}: schema hint does not resolve: {schemaLocation}");
			}

			while (reader.Read())
			{
			}
		}
		catch (Exception ex)
		{
			failures.Add($"{label}: XML schema validation failed: {ex.Message}");
			return;
		}

		failures.AddRange(validationMessages.Select(message => $"{label}: {message}"));
	}

	private static bool SchemaHintExists(string xmlPath, string schemaLocation)
	{
		if (Uri.TryCreate(schemaLocation, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			return true;
		}

		var resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(xmlPath)!, schemaLocation));
		return File.Exists(resolvedPath);
	}

	private static ExampleBuildExpectation Expect(string projectName, params (string Id, int Count)[] diagnostics)
	{
		var result = new ExampleBuildExpectation(projectName, ExampleConfigStyle.InlineInExample, diagnostics.ToDictionary(diagnostic => diagnostic.Id, diagnostic => diagnostic.Count, StringComparer.Ordinal));

		return result;
	}

	private static ExampleBuildExpectation ExpectFile(string projectName, params (string Id, int Count)[] diagnostics)
	{
		var result = new ExampleBuildExpectation(projectName, ExampleConfigStyle.SettingsFile, diagnostics.ToDictionary(diagnostic => diagnostic.Id, diagnostic => diagnostic.Count, StringComparer.Ordinal));

		return result;
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

	private enum ExampleConfigStyle
	{
		InlineInExample,
		SettingsFile
	}

	private sealed record ExampleBuildExpectation(string RelativeProjectPath, ExampleConfigStyle ConfigStyle, IReadOnlyDictionary<string, int> Diagnostics);
}
