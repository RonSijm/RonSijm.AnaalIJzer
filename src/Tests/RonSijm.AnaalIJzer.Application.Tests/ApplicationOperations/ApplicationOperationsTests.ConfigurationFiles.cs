using RonSijm.AnaalIJzer.Outputs.Documentation;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_FormatsArchitectureConfiguration()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-format-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			await File.WriteAllTextAsync(configPath, """<ArchitecturalLevels><Layer name="Application"><Class endsWith="Service" /></Layer><Layer name="Repository"><Class endsWith="Repository" /></Layer><AllowedDependency from="Application" to="Repository" /></ArchitecturalLevels>""", cancellationToken);

			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.FormatConfig)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [configPath]
			}, cancellationToken);

			result.OutputPath.Should().Be(configPath);
			var formatted = await File.ReadAllTextAsync(configPath, cancellationToken);
			formatted.Should().Contain("<ArchitecturalLevels>");
			formatted.Should().Contain("  <Layer name=\"Application\">");
			formatted.Should().Contain("    <Class endsWith=\"Service\" />");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_ExplainsArchitectureConfiguration()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-explain-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var outputPath = Path.Combine(tempDirectory, "explanation.md");
			await File.WriteAllTextAsync(configPath, """
			                                         <ArchitecturalLevels requireRecognizedDependencies="Constructor">
			                                           <Layer name="Application" description="Application services">
			                                             <Class endsWith="Service" />
			                                           </Layer>
			                                           <Layer name="Repository">
			                                             <Class endsWith="Repository" />
			                                           </Layer>
			                                           <AllowedDependency from="Application" to="Repository" allowedSites="Constructor" />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);

			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.ExplainConfig)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputPath
			}, cancellationToken);

			result.OutputPath.Should().Be(outputPath);
			result.Content.Should().Contain("# Architecture Configuration Explanation");
			result.Content.Should().Contain("Layer `Application`");
			result.Content.Should().Contain("Description: Application services");
			result.Content.Should().Contain("Dependency rule allows `Application` -> `Repository`");
			var explanation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			explanation.Should().Be(result.Content);
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_GeneratesProjectDocumentationWithRuleMatchesAndViolations()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-evidence-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FindSchemaPath())!, "..", "..", "..", ".."));
			var projectPath = Path.Combine(repositoryRoot, "Examples", "Scenarios", "Example.RepositoryQuerySurface", "Example.RepositoryQuerySurface.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-documentation.md");
			await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.Project,
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
	public async Task ApplicationRunner_GeneratesVisibilityPolicyCodeEvidence()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-visibility-evidence-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var projectPath = FindRepositoryProject(
				"Examples",
				"Diagnostics",
				"Example.Arch012.VisibilityPolicy",
				"Example.Arch012.VisibilityPolicy.csproj");
			var outputPath = Path.Combine(tempDirectory, "architecture-documentation.md");
			await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				OutputPath = outputPath,
				IncludeCodeEvidence = true
			}, cancellationToken);

			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("### Visibility Policy Declarations");
			documentation.Should().Contain("**passes** `Example.Arch012.VisibilityPolicy.LollyQueryable`");
			documentation.Should().Contain("**violates** `Example.Arch012.VisibilityPolicy.SourLollyQueryable`");
			documentation.Should().Contain("not effectively external");
			documentation.Should().Contain("externally visible");
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
	public async Task ApplicationRunner_GeneratesDocumentationFromXmlWithIncludes()
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

			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
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
	public async Task ApplicationRunner_GeneratesDocumentationFromXmlWithWildcardIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-wildcard-tooling-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var pluginsDirectory = Path.Combine(tempDirectory, "RulePlugins");
			Directory.CreateDirectory(pluginsDirectory);
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var layersPath = Path.Combine(pluginsDirectory, "RestaurantLayers.anl");
			var flowPath = Path.Combine(pluginsDirectory, "RestaurantFlow.anl");
			var outputPath = Path.Combine(tempDirectory, "architecture.md");
			await File.WriteAllTextAsync(configPath, """
			                                         <ArchitecturalLevels description="Root settings">
			                                           <Include path="*.anl" description="Load every visible rule pack." />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);
			await File.WriteAllTextAsync(layersPath, """
			                                          <ArchitecturalLevels>
			                                            <Layer name="Waiter" description="Takes the order">
			                                              <Class endsWith="Waiter" />
			                                            </Layer>
			                                            <Layer name="Chef" description="Prepares the meal">
			                                              <Class endsWith="Chef" />
			                                            </Layer>
			                                            <Layer name="Pantry" description="Stores ingredients">
			                                              <Class endsWith="Pantry" />
			                                            </Layer>
			                                          </ArchitecturalLevels>
			                                          """, cancellationToken);
			await File.WriteAllTextAsync(flowPath, """
			                                        <ArchitecturalLevels>
			                                          <AllowedDependency from="Waiter" to="Chef" description="Waiters call chefs." />
			                                          <AllowedDependency from="Chef" to="Pantry" description="Chefs can fetch ingredients." />
			                                        </ArchitecturalLevels>
			                                        """, cancellationToken);

			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Documentation)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [configPath],
				OutputPath = outputPath
			}, cancellationToken);

			result.OutputPath.Should().Be(outputPath);
			var documentation = await File.ReadAllTextAsync(outputPath, cancellationToken);
			documentation.Should().Contain("Waiter");
			documentation.Should().Contain("Chef");
			documentation.Should().Contain("Pantry");
			documentation.Should().Contain("Waiters call chefs");
			documentation.Should().Contain("Chefs can fetch ingredients");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}

	[Fact]
	public async Task ApplicationRunner_MergesConfigurationsWithWildcardIncludes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = Path.Combine(Path.GetTempPath(), $"AnaalIJzer-merge-wildcard-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDirectory);

		try
		{
			var pluginsDirectory = Path.Combine(tempDirectory, "RulePlugins");
			Directory.CreateDirectory(pluginsDirectory);
			var firstPath = Path.Combine(tempDirectory, "Architecture.anl");
			var secondPath = Path.Combine(tempDirectory, "ExtraRules.anl");
			var layersPath = Path.Combine(pluginsDirectory, "RestaurantLayers.anl");
			var flowPath = Path.Combine(pluginsDirectory, "RestaurantFlow.anl");
			var outputPath = Path.Combine(tempDirectory, "Merged.anl");
			await File.WriteAllTextAsync(firstPath, """
			                                         <ArchitecturalLevels>
			                                           <Include path="*.anl" />
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);
			await File.WriteAllTextAsync(layersPath, """
			                                          <ArchitecturalLevels>
			                                            <Layer name="Waiter"><Class endsWith="Waiter" /></Layer>
			                                            <Layer name="Chef"><Class endsWith="Chef" /></Layer>
			                                          </ArchitecturalLevels>
			                                          """, cancellationToken);
			await File.WriteAllTextAsync(flowPath, """
			                                        <ArchitecturalLevels>
			                                          <AllowedDependency from="Waiter" to="Chef" />
			                                        </ArchitecturalLevels>
			                                        """, cancellationToken);
			await File.WriteAllTextAsync(secondPath, """
			                                         <ArchitecturalLevels>
			                                           <Layer name="Pantry"><Class endsWith="Pantry" /></Layer>
			                                         </ArchitecturalLevels>
			                                         """, cancellationToken);

			await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.MergeConfig)
			{
				InputKind = ApplicationInputKind.ConfigurationFile,
				InputPaths = [firstPath, secondPath],
				OutputPath = outputPath
			}, cancellationToken);

			var merged = await File.ReadAllTextAsync(outputPath, cancellationToken);
			merged.Should().Contain("<Layer name=\"Waiter\">");
			merged.Should().Contain("<Layer name=\"Chef\">");
			merged.Should().Contain("<Layer name=\"Pantry\">");
			merged.Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Chef\" />");
		}
		finally
		{
			Directory.Delete(tempDirectory, true);
		}
	}
}

