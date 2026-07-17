using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Tooling.Tests.Tooling;
public sealed partial class ToolingTests
{
	[Fact]
	public async Task ToolRunner_MergesConfigurationsAndFlattensIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-merge-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var firstPath = Path.Combine(tempDirectory, "First.anl");
			var includedPath = Path.Combine(tempDirectory, "Included.anl");
			var secondPath = Path.Combine(tempDirectory, "Second.anl");
			var outputPath = Path.Combine(tempDirectory, "Merged.anl");
			var schemaPath = FindSchemaPath();
			await File.WriteAllTextAsync(firstPath, $"""
			                                         <ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
			                                                              xsi:noNamespaceSchemaLocation="{schemaPath}"
			                                                              requireRecognizedDependencies="Constructor"
			                                                              enforceAcyclic="true">
			                                          <Layer name="Controller" requireRecognizedDependencies="Method"><Class startsWith="Api" endsWith="Controller" typeKind="Class" /></Layer>
			                                          <Include path="Included.anl" />
			                                        </ArchitecturalLevels>
			                                        """, cancellationToken);
			await File.WriteAllTextAsync(includedPath, """
			                                           <ArchitecturalLevels>
			                                             <Layer name="Application"><Class endsWith="Service" /></Layer>
			                                             <AllowedDependency from="Controller" to="Application" />
			                                           </ArchitecturalLevels>
			                                           """, cancellationToken);
			await File.WriteAllTextAsync(secondPath, """
			                                         <ArchitecturalLevels requireRecognizedDependencies="Local">
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
			merged.Root!.Attribute("requireRecognizedDependencies")?.Value.Should().Be("Constructor, Local");
			merged.Root.Attribute("enforceAcyclic")?.Value.Should().Be("true");
			merged.Root.Elements("Include").Should().BeEmpty();
			merged.Root.Elements("Layer").Select(element => element.Attribute("name")?.Value).Should().Equal("Controller", "Application", "Repository");
			merged.Root.Elements("Layer").First().Attribute("requireRecognizedDependencies")!.Value.Should().Be("Method");
			var controllerMatcher = merged.Root.Elements("Layer").First().Element("Class")!;
			controllerMatcher.Attribute("startsWith")!.Value.Should().Be("Api");
			controllerMatcher.Attribute("endsWith")!.Value.Should().Be("Controller");
			controllerMatcher.Attribute("typeKind")!.Value.Should().Be("Class");
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
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var outputDirectory = Path.Combine(tempDirectory, "Split");
			var schemaPath = FindSchemaPath();
			await File.WriteAllTextAsync(configPath, $"""
			                                          <ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
			                                                               xsi:noNamespaceSchemaLocation="{schemaPath}"
			                                                               requireRecognizedDependencies="Constructor">
			                                           <Forbidden><Class endsWith="Store" /></Forbidden>
			                                           <Layer name="Controller"><Class endsWith="Controller" /></Layer>
			                                           <Layer name="Application"><Class endsWith="Service" /></Layer>
			                                           <AllowedDependency from="Controller" to="Application" />
			                                           <Layer name="Repository"><Class startsWith="Order" endsWith="Repository" typeKind="Class" /></Layer>
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
			var generatedFiles = Directory.GetFiles(outputDirectory, "*.anl");
			generatedFiles.Should().HaveCount(4);
			foreach (var generatedFile in generatedFiles)
			{
				AssertValid(XDocument.Load(generatedFile), schemaPath);
			}
			var repositoryMatcher = generatedFiles
				.Select(XDocument.Load)
				.SelectMany(document => document.Descendants("Layer"))
				.Single(layer => layer.Attribute("name")?.Value == "Repository")
				.Element("Class")!;
			repositoryMatcher.Attribute("startsWith")!.Value.Should().Be("Order");
			repositoryMatcher.Attribute("endsWith")!.Value.Should().Be("Repository");
			repositoryMatcher.Attribute("typeKind")!.Value.Should().Be("Class");

			var manifestPath = Path.Combine(outputDirectory, "Architecture.anl");
			var manifest = XDocument.Load(manifestPath);
			manifest.Root!.Elements("Include").Should().HaveCount(3);
			File.Exists(Path.Combine(outputDirectory, "Shared.anl")).Should().BeTrue();

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
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
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

			var graphDocuments = Directory.GetFiles(outputDirectory, "Graph.*.anl").Select(XDocument.Load).ToArray();
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
}
