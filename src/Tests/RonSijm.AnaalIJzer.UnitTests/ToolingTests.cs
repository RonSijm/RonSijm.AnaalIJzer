using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.Tooling;

namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class ToolingTests
{
	[Fact]
	public void OperationCatalog_DescribesEveryToolCapability()
	{
		ToolOperationCatalog.All.Select(operation => operation.Kind).Should().Equal(
		[
			ToolOperationKind.GenerateConfig,
			ToolOperationKind.ExportConfig,
			ToolOperationKind.Documentation,
			ToolOperationKind.Report,
			ToolOperationKind.Inspect,
			ToolOperationKind.MergeConfig,
			ToolOperationKind.SplitConfig
		]);
		ToolOperationCatalog.All.Select(operation => operation.CommandName).Should().OnlyHaveUniqueItems();
		ToolInputCatalog.All.Select(input => input.OptionName).Should().OnlyHaveUniqueItems();

		var documentation = ToolOperationCatalog.Get(ToolOperationKind.Documentation);
		documentation.Supports(ToolInputKind.Project).Should().BeTrue();
		documentation.Supports(ToolInputKind.ConfigurationFile).Should().BeTrue();
		documentation.DefaultInput.Should().Be(ToolInputKind.ConfigurationFile);
		ToolOperationCatalog.All.Should().OnlyContain(operation => operation.Supports(operation.DefaultInput));
		ToolOperationCatalog.Get(ToolOperationKind.MergeConfig).SupportsMultipleInputs.Should().BeTrue();
		ToolOperationCatalog.Get(ToolOperationKind.SplitConfig).OutputKind.Should().Be(ToolOutputKind.Directory);
		ToolOperationCatalog.Find("inspect")!.Kind.Should().Be(ToolOperationKind.Inspect);
		ToolOperationCatalog.Find("validate")!.Kind.Should().Be(ToolOperationKind.Inspect);
		ToolInputPathParser.Parse("First.xml; Second.xml").Should().Equal("First.xml", "Second.xml");
	}

	[Fact]
	public async Task ConfigurationGenerator_InfersDominantEdgesAndGrandfathersMinorityCallers()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var compilation = CreateInferenceCompilation(managerCallerCount: 10, repositoryCallerStart: 10);
		var configurationText = ApplicationConfigurationGenerator.Generate(compilation, "AnaalIJzer.xsd", new ConfigurationGenerationOptions
		{
			Strategy = ConfigurationGenerationStrategy.Conventions,
			MinimumConfidence = 0.90,
			MinimumSupport = 5
		}, cancellationToken);
		var configuration = XDocument.Parse(configurationText);

		AssertValid(configuration, FindSchemaPath());
		configuration.Root!.Elements("AllowedDependency")
			.Select(edge => (From: edge.Attribute("from")?.Value, To: edge.Attribute("to")?.Value))
			.Should().Contain(("Controllers", "Managers")).And.NotContain(("Controllers", "Repositories"));
		configuration.Root.Elements("AllowedDependency").Single().Attribute("description")!.Value.Should().Contain("10 of 10 active Controllers callers (100%)");
		configuration.Descendants("Exceptions").Should().ContainSingle();
		configuration.Descendants("Exceptions").Elements("Class")
			.Should().ContainSingle(element => element.Attribute("exactFullName")!.Value == "CandyShop.Controllers.CandyController10");

		var diagnostics = await ApplicationConfigurationGenerator.ValidateAsync(compilation, configurationText, "ArchitecturalLevels.xml", cancellationToken);
		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void ConfigurationGenerator_PreservesAmbiguousEdges()
	{
		var compilation = CreateInferenceCompilation(managerCallerCount: 5, repositoryCallerStart: 6);
		var configurationText = ApplicationConfigurationGenerator.Generate(compilation, "AnaalIJzer.xsd", new ConfigurationGenerationOptions
		{
			Strategy = ConfigurationGenerationStrategy.Conventions,
			MinimumConfidence = 0.90,
			MinimumSupport = 5
		}, TestContext.Current.CancellationToken);
		var configuration = XDocument.Parse(configurationText);

		configuration.Root!.Elements("AllowedDependency")
			.Select(edge => (From: edge.Attribute("from")?.Value, To: edge.Attribute("to")?.Value))
			.Should().Contain(("Controllers", "Managers")).And.Contain(("Controllers", "Repositories"));
		configuration.Root.Elements("AllowedDependency").Should().OnlyContain(edge => edge.Attribute("description")!.Value.Contains("Preserved because this layer had no dependency edge"));
		configuration.Descendants("Exceptions").Should().BeEmpty();
	}

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
			var outputPath = Path.Combine(tempDirectory, "ArchitecturalLevels.xml");
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
			documentation.Should().Contain("`ArchitecturalLevels.xml`");
			documentation.Should().Contain("<Layer name=\"QuerySurface\">");
			documentation.Should().Contain("### Effective Matcher Rule Matches");
			documentation.Should().Contain("### Dependency Rule Usages");
			documentation.Should().Contain("AllowedDependency `Presentation -> Application`");
			documentation.Should().Contain("`OrderEndpoint` -> `OrderService` at `Constructor`");
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
	public async Task ToolRunner_ValidatesConfigurationAndReportsPermittedCycles()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-health-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "ArchitecturalLevels.xml");
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
			"<ArchitecturalLevels strict=\"true\" />",
			Path.Combine("settings", "ArchitecturalLevels.xml"));

		documentation.Should().Contain("## Input Configuration");
		documentation.Should().Contain("This documentation was generated from the following architecture configuration: `ArchitecturalLevels.xml`.");
		documentation.Should().Contain("````xml");
		documentation.Should().Contain("<ArchitecturalLevels strict=\"true\" />");
	}

	[Fact]
	public async Task ToolRunner_GeneratesDocumentationFromXmlWithIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-tooling-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "ArchitecturalLevels.xml");
			var includedPath = Path.Combine(tempDirectory, "Shared.xml");
			var outputPath = Path.Combine(tempDirectory, "architecture.md");
			await File.WriteAllTextAsync(configPath, """
			                                         <ArchitecturalLevels description="Root settings">
			                                           <Include path="Shared.xml" description="Shared layers" />
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

	[Fact]
	public async Task ToolRunner_MergesConfigurationsAndFlattensIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-merge-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var firstPath = Path.Combine(tempDirectory, "First.xml");
			var includedPath = Path.Combine(tempDirectory, "Included.xml");
			var secondPath = Path.Combine(tempDirectory, "Second.xml");
			var outputPath = Path.Combine(tempDirectory, "Merged.xml");
			var schemaPath = FindSchemaPath();
			await File.WriteAllTextAsync(firstPath, $"""
			                                         <ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
			                                                              xsi:noNamespaceSchemaLocation="{schemaPath}"
			                                                              strict="true"
			                                                              enforceAcyclic="true">
			                                          <Layer name="Controller"><Class endsWith="Controller" /></Layer>
			                                          <Include path="Included.xml" />
			                                        </ArchitecturalLevels>
			                                        """, cancellationToken);
			await File.WriteAllTextAsync(includedPath, """
			                                           <ArchitecturalLevels>
			                                             <Layer name="Application"><Class endsWith="Service" /></Layer>
			                                             <AllowedDependency from="Controller" to="Application" />
			                                           </ArchitecturalLevels>
			                                           """, cancellationToken);
			await File.WriteAllTextAsync(secondPath, """
			                                         <ArchitecturalLevels>
			                                           <Layer name="Repository"><Class endsWith="Repository" /></Layer>
			                                           <AllowedDependency from="Application" to="Repository" />
			                                           <BlockedDependency from="Controller" to="Repository" />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);

			await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.MergeConfig)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [firstPath, secondPath],
				OutputPath = outputPath
			}, cancellationToken);

			var merged = XDocument.Load(outputPath);
			AssertValid(merged, schemaPath);
			merged.Root!.Attribute("strict")?.Value.Should().Be("true");
			merged.Root.Attribute("enforceAcyclic")?.Value.Should().Be("true");
			merged.Root.Elements("Include").Should().BeEmpty();
			merged.Root.Elements("Layer").Select(element => element.Attribute("name")?.Value).Should().Equal("Controller", "Application", "Repository");
			merged.Root.Elements("AllowedDependency").Should().HaveCount(2);
			merged.Root.Elements("BlockedDependency").Should().ContainSingle();
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ToolRunner_SplitsDisconnectedGraphsAndWritesManifest()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-split-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "ArchitecturalLevels.xml");
			var outputDirectory = Path.Combine(tempDirectory, "Split");
			var schemaPath = FindSchemaPath();
			await File.WriteAllTextAsync(configPath, $"""
			                                          <ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
			                                                               xsi:noNamespaceSchemaLocation="{schemaPath}"
			                                                               strict="true">
			                                           <Forbidden><Class endsWith="Store" /></Forbidden>
			                                           <Layer name="Controller"><Class endsWith="Controller" /></Layer>
			                                           <Layer name="Application"><Class endsWith="Service" /></Layer>
			                                           <AllowedDependency from="Controller" to="Application" />
			                                           <Layer name="Repository"><Class endsWith="Repository" /></Layer>
			                                           <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			                                           <AllowedDependency from="Repository" to="QuerySurface" />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);

			var result = await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.SplitConfig)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputDirectory
			}, cancellationToken);

			result.Message.Should().Contain("2 dependency graphs");
			var generatedFiles = Directory.GetFiles(outputDirectory, "*.xml");
			generatedFiles.Should().HaveCount(4);
			foreach (var generatedFile in generatedFiles)
			{
				AssertValid(XDocument.Load(generatedFile), schemaPath);
			}

			var manifestPath = Path.Combine(outputDirectory, "ArchitecturalLevels.xml");
			var manifest = XDocument.Load(manifestPath);
			manifest.Root!.Elements("Include").Should().HaveCount(3);
			File.Exists(Path.Combine(outputDirectory, "Shared.xml")).Should().BeTrue();

			var documentationPath = Path.Combine(tempDirectory, "architecture.md");
			await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.Documentation)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [manifestPath],
				OutputPath = documentationPath
			}, cancellationToken);
			var documentation = await File.ReadAllTextAsync(documentationPath, cancellationToken);
			documentation.Should().Contain("Controller");
			documentation.Should().Contain("Application");
			documentation.Should().Contain("Repository");
			documentation.Should().Contain("QuerySurface");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ToolRunner_SplitsTopLevelBoundariesWithoutFlatteningNestedLayers()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-nested-split-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "ArchitecturalLevels.xml");
			var outputDirectory = Path.Combine(tempDirectory, "Split");
			var schemaPath = FindSchemaPath();
			await File.WriteAllTextAsync(configPath, $"""
			                                          <ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
			                                                               xsi:noNamespaceSchemaLocation="{schemaPath}">
			                                            <Layer name="Ordering">
			                                              <Namespace startsWith="Shop.Ordering" />
			                                              <Layer name="Application"><Class endsWith="Service" /></Layer>
			                                              <Layer name="Repository"><Class endsWith="Repository" /></Layer>
			                                              <AllowedDependency from="Application" to="Repository" />
			                                            </Layer>
			                                            <Layer name="Billing">
			                                              <Namespace startsWith="Shop.Billing" />
			                                              <Layer name="Contracts"><Class endsWith="Contract" /></Layer>
			                                            </Layer>
			                                          </ArchitecturalLevels>
			                                          """, cancellationToken);

			await new ToolRunner().ExecuteAsync(new ToolRequest(ToolOperationKind.SplitConfig)
			{
				InputKind = ToolInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputDirectory
			}, cancellationToken);

			var graphDocuments = Directory.GetFiles(outputDirectory, "Graph.*.xml").Select(XDocument.Load).ToArray();
			graphDocuments.Should().HaveCount(2);
			var ordering = graphDocuments.Single(document => document.Root!.Elements("Layer").Any(layer => layer.Attribute("name")?.Value == "Ordering"));
			var orderingLayer = ordering.Root!.Element("Layer")!;
			orderingLayer.Elements("Layer").Select(layer => layer.Attribute("name")?.Value).Should().Equal("Application", "Repository");
			orderingLayer.Elements("AllowedDependency").Should().ContainSingle();
			AssertValid(ordering, schemaPath);
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	private static string FindSchemaPath()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			var candidate = Path.Combine(directory.FullName, "src", "Main", "RonSijm.AnaalIJzer", "Scheme", "AnaalIJzer.xsd");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		throw new InvalidOperationException("Could not locate AnaalIJzer.xsd.");
	}

	private static CSharpCompilation CreateInferenceCompilation(int managerCallerCount, int repositoryCallerStart)
	{
		var controllers = Enumerable.Range(1, 10).Select(index =>
		{
			var dependency = index <= managerCallerCount ? "CandyShop.Managers.CandyManager" : "CandyShop.Repositories.CandyRepository";
			if (index >= repositoryCallerStart)
			{
				dependency = index <= managerCallerCount
					? $"CandyShop.Managers.CandyManager manager, CandyShop.Repositories.CandyRepository repository"
					: "CandyShop.Repositories.CandyRepository repository";
			}
			else
			{
				dependency += " dependency";
			}

			return $"namespace CandyShop.Controllers {{ public sealed class CandyController{index} {{ public CandyController{index}({dependency}) {{ }} }} }}";
		});
		var source = string.Join(Environment.NewLine, controllers) + """

			namespace CandyShop.Managers { public sealed class CandyManager { } }
			namespace CandyShop.Repositories { public sealed class CandyRepository { } }
			""";
		return CSharpCompilation.Create(
			"CandyShop",
			[CSharpSyntaxTree.ParseText(source)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static void AssertValid(XDocument document, string schemaPath)
	{
		var schemas = new XmlSchemaSet();
		schemas.Add(null, schemaPath);
		var errors = new List<string>();
		document.Validate(schemas, (_, args) => errors.Add(args.Message));
		errors.Should().BeEmpty();
	}
}
