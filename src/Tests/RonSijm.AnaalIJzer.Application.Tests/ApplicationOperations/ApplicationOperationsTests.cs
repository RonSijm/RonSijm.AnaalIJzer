using System.Xml.Linq;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public void OperationCatalog_DescribesEveryToolCapability()
	{
		ApplicationOperationCatalog.All.Select(operation => operation.Kind).Should().Equal(
		[
			ApplicationOperationKind.GenerateConfig,
			ApplicationOperationKind.ExportConfig,
			ApplicationOperationKind.Documentation,
			ApplicationOperationKind.Report,
			ApplicationOperationKind.Inspect,
			ApplicationOperationKind.Fixes,
			ApplicationOperationKind.ApplyFix,
			ApplicationOperationKind.MergeConfig,
			ApplicationOperationKind.SplitConfig,
			ApplicationOperationKind.FormatConfig,
			ApplicationOperationKind.ExplainConfig
		]);
		ApplicationOperationCatalog.All.Select(operation => operation.CommandName).Should().OnlyHaveUniqueItems();
		ApplicationInputCatalog.All.Select(input => input.OptionName).Should().OnlyHaveUniqueItems();

		var documentation = ApplicationOperationCatalog.Get(ApplicationOperationKind.Documentation);
		documentation.Supports(ApplicationInputKind.Project).Should().BeTrue();
		documentation.Supports(ApplicationInputKind.ConfigurationFile).Should().BeTrue();
		documentation.DefaultInput.Should().Be(ApplicationInputKind.ConfigurationFile);
		var generateConfig = ApplicationOperationCatalog.Get(ApplicationOperationKind.GenerateConfig);
		generateConfig.Supports(ApplicationInputKind.Project).Should().BeTrue();
		generateConfig.Supports(ApplicationInputKind.Solution).Should().BeTrue();
		var report = ApplicationOperationCatalog.Get(ApplicationOperationKind.Report);
		report.Supports(ApplicationInputKind.Project).Should().BeTrue();
		report.Supports(ApplicationInputKind.Solution).Should().BeTrue();
		var inspect = ApplicationOperationCatalog.Get(ApplicationOperationKind.Inspect);
		inspect.Supports(ApplicationInputKind.Project).Should().BeTrue();
		inspect.Supports(ApplicationInputKind.Solution).Should().BeTrue();
		inspect.Supports(ApplicationInputKind.ConfigurationFile).Should().BeTrue();
		var fixes = ApplicationOperationCatalog.Get(ApplicationOperationKind.Fixes);
		fixes.Supports(ApplicationInputKind.Project).Should().BeTrue();
		fixes.Supports(ApplicationInputKind.Solution).Should().BeTrue();
		var applyFix = ApplicationOperationCatalog.Get(ApplicationOperationKind.ApplyFix);
		applyFix.Supports(ApplicationInputKind.Project).Should().BeTrue();
		applyFix.Supports(ApplicationInputKind.Solution).Should().BeTrue();
		ApplicationOperationCatalog.All.Should().OnlyContain(operation => operation.Supports(operation.DefaultInput));
		ApplicationOperationCatalog.Get(ApplicationOperationKind.MergeConfig).SupportsMultipleInputs.Should().BeTrue();
		ApplicationOperationCatalog.Get(ApplicationOperationKind.SplitConfig).OutputKind.Should().Be(ApplicationOutputKind.Directory);
		ApplicationOperationCatalog.Find("inspect")!.Kind.Should().Be(ApplicationOperationKind.Inspect);
		ApplicationOperationCatalog.Find("validate")!.Kind.Should().Be(ApplicationOperationKind.Inspect);
		ApplicationOperationCatalog.Find("config-fixes")!.Kind.Should().Be(ApplicationOperationKind.Fixes);
		ApplicationInputCatalog.Get(ApplicationInputKind.Solution).OptionName.Should().Be("--solution");
		ApplicationInputPathParser.Parse("First.xml; Second.xml").Should().Equal("First.xml", "Second.xml");
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

		var diagnostics = await ApplicationConfigurationGenerator.ValidateAsync(compilation, configurationText, "Architecture.anl", cancellationToken);
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

	[Theory]
	[InlineData(0.75, 5, 1, 1, 2)]
	[InlineData(0.90, 5, 2, 0, 0)]
	[InlineData(0.75, 9, 2, 0, 0)]
	[InlineData(0.20, 2, 2, 2, 0)]
	public void ConfigurationGenerator_AppliesDifferentThresholdsToTheSameEvidence(double minimumConfidence, int minimumSupport, int expectedEdgeCount, int expectedConventionCount, int expectedExceptionCount)
	{
		var compilation = CreateThresholdComparisonCompilation();
		var configurationText = ApplicationConfigurationGenerator.Generate(compilation, "AnaalIJzer.xsd", new ConfigurationGenerationOptions
		{
			Strategy = ConfigurationGenerationStrategy.Conventions,
			MinimumConfidence = minimumConfidence,
			MinimumSupport = minimumSupport
		}, TestContext.Current.CancellationToken);
		var configuration = XDocument.Parse(configurationText);
		var edges = configuration.Root!.Elements("AllowedDependency").ToArray();

		AssertValid(configuration, FindSchemaPath());
		edges.Should().HaveCount(expectedEdgeCount);
		edges.Should().ContainSingle(edge =>
			edge.Attribute("from")!.Value == "Presentation"
			&& edge.Attribute("to")!.Value == "Application"
			&& edge.Attribute("description")!.Value.Contains("8 of 10 active Presentation callers"));
		if (expectedEdgeCount == 1)
		{
			edges.Should().NotContain(edge => edge.Attribute("to")!.Value == "Persistence");
		}
		else
		{
			edges.Should().ContainSingle(edge =>
				edge.Attribute("from")!.Value == "Presentation"
				&& edge.Attribute("to")!.Value == "Persistence"
				&& edge.Attribute("description")!.Value.Contains("2 of 10 active Presentation callers"));
		}

		edges.Count(edge => edge.Attribute("description")!.Value.StartsWith("Inferred convention:", StringComparison.Ordinal))
			.Should().Be(expectedConventionCount);
		var exceptions = configuration.Descendants("Exceptions").Elements("Class").ToArray();
		exceptions.Should().HaveCount(expectedExceptionCount);
		if (expectedExceptionCount > 0)
		{
			exceptions.Select(exception => exception.Attribute("exactFullName")!.Value)
				.Should().BeEquivalentTo("Shop.Presentation.LegacyAdminEndpoint", "Shop.Presentation.ImportEndpoint");
		}
	}

}

