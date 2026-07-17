using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Tooling.Tests.Tooling;
public sealed partial class ToolingTests
{
	[Fact]
	public async Task ToolRunner_GeneratesCleanConfigurationFromRealProject()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-generate-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var projectPath = Path.Combine(repositoryRoot, "Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var outputPath = Path.Combine(tempDirectory, "Architecture.anl");
			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.GenerateConfig)
			{
				InputKind = ToolInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = outputPath,
				GenerateDocumentation = true,
				IncludeDocumentationInput = true
			}, cancellationToken);

			result.Message.Should().Contain("Generated configuration");
			result.Message.Should().Contain("Generated code-backed documentation");
			File.Exists(Path.Combine(tempDirectory, "AnaalIJzer.xsd")).Should().BeTrue();
			var documentation = await File.ReadAllTextAsync(Path.Combine(tempDirectory, "architecture-documentation.md"), cancellationToken);
			documentation.Should().Contain("## Code Evidence");
			documentation.Should().Contain("## Input Configuration");
			documentation.Should().Contain("This documentation was generated from the following architecture configuration");
			documentation.Should().Contain("<ArchitecturalLevels");
			documentation.Should().Contain("OrderEndpoint");
			documentation.Should().Contain("The analyzer reports no violations");
			var configuration = XDocument.Load(outputPath);
			AssertValid(configuration, Path.Combine(tempDirectory, "AnaalIJzer.xsd"));
			configuration.Root!.Elements("Layer").Should().NotBeEmpty();
			configuration.Root.Elements("AllowedDependency").Should().NotBeEmpty();
			configuration.Root.Elements("AllowedDependency").Should().OnlyContain(element => element.Attribute("allowedSites") != null);
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ToolRunner_GeneratesProjectDocumentationWithRuleMatchesAndViolations()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-evidence-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var projectPath = Path.Combine(repositoryRoot, "Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-documentation.md");
			await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
			{
				InputKind = ToolInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = outputPath,
				IncludeCodeEvidence = true,
				IncludeDocumentationInput = true
			}, cancellationToken);

			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("## Code Evidence");
			documentation.Should().Contain("## Input Configuration");
			documentation.Should().Contain("`Architecture.anl`");
			documentation.Should().Contain("<Layer name=\"QuerySurface\">");
			documentation.Should().Contain("### Effective Matcher Rule Matches");
			documentation.Should().Contain("### Dependency Rule Usages");
			documentation.Should().Contain("AllowedDependency `Presentation -> Application`");
			documentation.Should().Contain("`Example.RepositoryQuerySurface.OrderEndpoint` -> `Example.RepositoryQuerySurface.OrderService` at `Constructor`");
			documentation.Should().Contain("OrderService");
			documentation.Should().Contain("### Current Rule Violations");
			documentation.Should().Contain("`ARCH001`");
			documentation.Should().Contain("OrderDashboardService");
			documentation.Should().Contain("Local");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ToolRunner_InspectsReleaseProjectWithoutRunningSourceLink()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-inspect-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var projectPath = Path.Combine(repositoryRoot, "src", "Main", "RonSijm.AnaalIJzer", "RonSijm.AnaalIJzer.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-health.md");
			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Inspect)
			{
				InputKind = ToolInputKind.Project,
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
	public async Task ToolRunner_InspectsSolutionWithoutWritingOutput()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-inspect-solution-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject("Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Inspect)
			{
				InputKind = ToolInputKind.Solution,
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
	public async Task ToolRunner_GeneratesSolutionViolationReport()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-report-solution-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject("Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var outputPath = Path.Combine(tempDirectory, "architectural-violations.md");
			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Report)
			{
				InputKind = ToolInputKind.Solution,
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
	public async Task ArchitectureGraphWorkspaceSnapshotLoader_LoadsSolutionGraphAndCodeEvidence()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-graph-solution-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject("Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var solutionPath = WriteSolutionFile(tempDirectory, projectPath);
			var snapshot = await new ArchitectureGraphWorkspaceSnapshotLoader("Release").LoadAsync(solutionPath, cancellationToken);

			snapshot.HasConfiguration.Should().BeTrue();
			snapshot.ConfigurationSource.Path.Should().EndWith("Architecture.anl");
			snapshot.Layers.Select(layer => layer.Path).Should().Contain(["Presentation", "Application", "Persistence", "QuerySurface", "Projection"]);
			snapshot.Evidence.HasEvidence.Should().BeTrue();
			snapshot.Evidence.Types.Should().Contain(type => type.LayerPath == "Application" && type.TypeName == "OrderService");
			snapshot.Evidence.Dependencies.Should().Contain(dependency =>
				dependency.DiagnosticId == "ARCH001"
				&& dependency.CallerTypeName == "OrderService"
				&& dependency.DependencyTypeName == "OrderQuery"
				&& dependency.Site == "Local");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ToolRunner_HealthInspectionEvaluatesEveryCombinedMatcherCondition()
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
		var projectPath = Path.Combine(repositoryRoot, "Examples", "Features", "Example.CombinedMatchers", "Example.CombinedMatchers.csproj");
		var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Inspect)
		{
			InputKind = ToolInputKind.Project,
			InputPaths = [projectPath],
			WriteOutput = false
		}, TestContext.Current.CancellationToken);

		result.Content.Should().Contain("ARCH005");
		result.Content.Should().NotContain("| Warning | Unmatched matcher |");
	}

	[Fact]
	public async Task ToolRunner_ValidatesConfigurationAndReportsPermittedCycles()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-health-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

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

			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Inspect)
			{
				InputKind = ToolInputKind.ConfigurationFile,
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
	public void DocumentationInputAppender_IncludesTheSourceXmlAndContext()
	{
		var documentation = ArchitectureDocumentationInputAppender.Append(
			"# Architecture Documentation" + Environment.NewLine,
			"<ArchitecturalLevels requireRecognizedDependencies=\"Constructor, Local\" />",
			Path.Combine("settings", "Architecture.anl"));

		documentation.Should().Contain("## Input Configuration");
		documentation.Should().Contain("This documentation was generated from the following architecture configuration: `Architecture.anl`.");
		documentation.Should().Contain("````xml");
		documentation.Should().Contain("<ArchitecturalLevels requireRecognizedDependencies=\"Constructor, Local\" />");
	}

	[Fact]
	public async Task ToolRunner_GeneratesDocumentationFromXmlWithIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-tooling-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var includedPath = Path.Combine(tempDirectory, "Shared.anl");
			var outputPath = Path.Combine(tempDirectory, "architecture.md");
			await File.WriteAllTextAsync(configPath, """
			                                         <ArchitecturalLevels description="Root settings">
			                                           <Include path="Shared.anl" description="Shared layers" />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);
			await File.WriteAllTextAsync(includedPath, """
			                                           <ArchitecturalLevels>
			                                             <Layer name="Application" description="Application services">
			                                               <Class endsWith="Service" description="Service types" />
			                                             </Layer>
			                                             <Layer name="Repository" description="Persistence">
			                                               <Class endsWith="Repository" description="Repository types" />
			                                             </Layer>
			                                             <AllowedDependency from="Application" to="Repository" description="Services may use repositories" />
			                                           </ArchitecturalLevels>
			                                           """, cancellationToken);

			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputPath
			}, cancellationToken);

			result.OutputPath.Should().Be(outputPath);
			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("Application");
			documentation.Should().Contain("Repository");
			documentation.Should().Contain("Services may use repositories");
			documentation.Should().NotContain("## Code Evidence");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	private static string FindRepositoryProject(params string[] pathParts)
	{
		var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
		var segments = new string[pathParts.Length + 1];
		segments[0] = repositoryRoot;
		Array.Copy(pathParts, 0, segments, 1, pathParts.Length);
		var result = Path.Combine(segments);

		return result;
	}

	private static string WriteSolutionFile(string directory, string projectPath)
	{
		var solutionPath = Path.Combine(directory, "ExampleSolution.slnx");
		new XDocument(new XElement("Solution", new XElement("Project", new XAttribute("Path", projectPath)))).Save(solutionPath);

		return solutionPath;
	}

}
