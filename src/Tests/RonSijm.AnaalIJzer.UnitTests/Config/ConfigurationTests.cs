using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Parsing;
using RonSijm.AnaalIJzer.UnitTests.TestSupport;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.UnitTests.Config;

public sealed class ConfigurationTests
{
	[Fact]
	public async Task NoConfigFile_NoDiagnostics()
	{
		const string source = """
		                      public class OtherController { }
		                      public class PatientController(OtherController other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task InlineAssemblyMetadataSettings_ReportsDiagnostics()
	{
		const string source = """"
		                      using System.Reflection;

		                      [assembly: AssemblyMetadata("AnaalIJzerSettings", """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Application">
		                          <Class endsWith="Kitchen" />
		                        </Layer>
		                        <Layer name="Repository">
		                          <Class endsWith="Repository" />
		                        </Layer>
		                        <AllowedDependency from="Controller" to="Application" />
		                        <AllowedDependency from="Application" to="Repository" />
		                      </ArchitecturalLevels>
		                      """)]

		                      public interface IPizzaKitchen { }
		                      public interface ICheeseRepository { }
		                      public class PizzaController(IPizzaKitchen kitchen) { }
		                      public class MenuController(ICheeseRepository cheeseRepository) { }
		                      """";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle()
			.Which.GetMessage(CultureInfo.InvariantCulture).Should().Contain("MenuController");
	}

	[Fact]
	public async Task ArchitecturalLevelsAdditionalFile_TakesPrecedenceOverInlineAssemblyMetadata()
	{
		const string inlineConfigSource = """"
		                                  using System.Reflection;

		                                  [assembly: AssemblyMetadata("AnaalIJzerSettings", """
		                                  <ArchitecturalLevels>
		                                    <Layer name="Controller">
		                                      <Class endsWith="Controller" />
		                                    </Layer>
		                                    <Layer name="Repository">
		                                      <Class endsWith="Repository" />
		                                    </Layer>
		                                  </ArchitecturalLevels>
		                                  """)]

		                                  public interface ICheeseRepository { }
		                                  public class MenuController(ICheeseRepository cheeseRepository) { }
		                                  """";

		const string additionalFileConfig = """
		                                    <ArchitecturalLevels>
		                                      <Layer name="Controller">
		                                        <Class endsWith="Controller" />
		                                      </Layer>
		                                      <Layer name="Repository">
		                                        <Class endsWith="Repository" />
		                                      </Layer>
		                                      <AllowedDependency from="Controller" to="Repository" />
		                                    </ArchitecturalLevels>
		                                    """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(inlineConfigSource, additionalFileConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task NonDefaultAdditionalSettingsFile_IsIgnoredAsTopLevelConfig()
	{
		const string source = """
		                      public interface ICheeseRepository { }
		                      public class MenuController(ICheeseRepository cheeseRepository) { }
		                      """;

		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Repository">
		                          <Class endsWith="Repository" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, ("RestaurantRules.anl", config));

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ArchitectureAnlAdditionalFile_IsUsedAsTopLevelConfig()
	{
		const string source = """
		                      public interface ICheeseRepository { }
		                      public class MenuController(ICheeseRepository cheeseRepository) { }
		                      """;

		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Repository">
		                          <Class endsWith="Repository" />
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, ("Architecture.anl", config));

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle()
			.Which.GetMessage(CultureInfo.InvariantCulture).Should().Contain("MenuController");
	}

	[Fact]
	public async Task IncludeElement_MergesAdditionalSettingsFile()
	{
		const string parentConfig = """
		                            <ArchitecturalLevels>
		                              <Include path="SharedPizzeriaLayers.xml" />
		                              <AllowedDependency from="Controller" to="Application" />
		                            </ArchitecturalLevels>
		                            """;

		const string sharedConfig = """
		                            <ArchitecturalLevels>
		                              <Layer name="Controller">
		                                <Class endsWith="Controller" />
		                              </Layer>
		                              <Layer name="Application">
		                                <Class endsWith="Kitchen" />
		                              </Layer>
		                              <Layer name="Repository">
		                                <Class endsWith="Repository" />
		                              </Layer>
		                              <AllowedDependency from="Application" to="Repository" />
		                            </ArchitecturalLevels>
		                            """;

		const string source = """
		                      public interface IPizzaKitchen { }
		                      public interface ICheeseRepository { }
		                      public class PizzaController(IPizzaKitchen kitchen) { }
		                      public class PizzaKitchen(ICheeseRepository cheeseRepository) { }
		                      public class MenuController(ICheeseRepository cheeseRepository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			source,
			("Architecture.anl", parentConfig),
			("SharedPizzeriaLayers.xml", sharedConfig));

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle()
			.Which.GetMessage(CultureInfo.InvariantCulture).Should().Contain("MenuController");
	}

	[Fact]
	public async Task IncludeElement_MergesNestedSettingsFile()
	{
		const string parentConfig = """
		                            <ArchitecturalLevels>
		                              <Include path="SharedPizzeriaLayers.xml" />
		                              <AllowedDependency from="Controller" to="Application" />
		                            </ArchitecturalLevels>
		                            """;

		const string sharedConfig = """
		                            <ArchitecturalLevels>
		                              <Include path="SharedPizzeriaEdges.xml" />
		                              <Layer name="Controller">
		                                <Class endsWith="Controller" />
		                              </Layer>
		                              <Layer name="Application">
		                                <Class endsWith="Kitchen" />
		                              </Layer>
		                              <Layer name="Repository">
		                                <Class endsWith="Repository" />
		                              </Layer>
		                            </ArchitecturalLevels>
		                            """;

		const string sharedEdgesConfig = """
		                                 <ArchitecturalLevels>
		                                   <AllowedDependency from="Application" to="Repository" />
		                                 </ArchitecturalLevels>
		                                 """;

		const string source = """
		                      public interface IPizzaKitchen { }
		                      public interface ICheeseRepository { }
		                      public class PizzaController(IPizzaKitchen kitchen) { }
		                      public class PizzaKitchen(ICheeseRepository cheeseRepository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			source,
			("Architecture.anl", parentConfig),
			("SharedPizzeriaLayers.xml", sharedConfig),
			("SharedPizzeriaEdges.xml", sharedEdgesConfig));

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task CustomLayerNames_AreRespected()
	{
		const string customConfig = """
		                            <ArchitecturalLevels>
		                                <Layer name="Handler">
		                                    <Class endsWith="Handler" />
		                                </Layer>
		                                <Layer name="Service">
		                                    <Class endsWith="Service" />
		                                </Layer>
		                                <AllowedDependency from="Handler" to="Service" />
		                            </ArchitecturalLevels>
		                            """;

		// OtherHandler and RequestHandler are both in the Handler layer.
		const string source = """
		                      public class OtherHandler { }
		                      public class RequestHandler(OtherHandler other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, customConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task CustomLayerNames_ValidDependency_NoDiagnostic()
	{
		const string customConfig = """
		                            <ArchitecturalLevels>
		                                <Layer name="Handler">
		                                    <Class endsWith="Handler" />
		                                </Layer>
		                                <Layer name="Service">
		                                    <Class endsWith="Service" />
		                                </Layer>
		                                <AllowedDependency from="Handler" to="Service" />
		                            </ArchitecturalLevels>
		                            """;

		const string source = """
		                      public class PatientService { }
		                      public class RequestHandler(PatientService service) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, customConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task DiagnosticMessage_ContainsCallerAndDependency()
	{
		// Same-layer (Controller -> Controller) — exercises the ARCH005 message template.
		const string source = """
		                      public class OtherController { }
		                      public class PatientController(OtherController other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		var msg = diagnostics
			.First(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.GetMessage(CultureInfo.InvariantCulture);

		msg.Should().Contain("PatientController");
		msg.Should().Contain("OtherController");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("<ArchitecturalLevels>")]
	public void Parser_ReturnsEmptyConfig_WhenAdditionalFileHasNoUsableXml(string configText)
	{
		var config = ParseConfig(configText);

		config.HasLayers.Should().BeFalse();
		config.LayerNames.Should().BeEmpty();
		config.AllowedEdges.Should().BeEmpty();
	}

	[Fact]
	public void Parser_SkipsUnnamedLayersAndIncompleteAllowedDependencies()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                              <Layer>
		                                  <Class endsWith="Ghost" />
		                              </Layer>
		                              <Layer name="Application">
		                                  <Class exactName="PizzaKitchen" />
		                                  <Class />
		                              </Layer>
		                              <Layer name="Repository">
		                                  <Class endsWith="Repository" />
		                              </Layer>
		                              <AllowedDependency from="Application" />
		                              <AllowedDependency to="Repository" />
		                              <AllowedDependency from="Application" to="Repository" />
		                          </ArchitecturalLevels>
		                          """;

		var config = ParseConfig(configText);

		config.LayerNames.Should().Equal("Application", "Repository");
		config.AllowedEdges.Should().ContainSingle()
			.Which.Should().Be(("Application", "Repository"));
		config.FindLayer("Ghost", string.Empty).Should().BeNull();

		var applicationMatch = config.FindLayer("PizzaKitchen", string.Empty);
		applicationMatch.Should().NotBeNull();
		applicationMatch!.Value.Layer.Name.Should().Be("Application");
	}

	[Fact]
	public void Parser_ReadsAllowedSites()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                              <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
		                              <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
		                              <AllowedDependency from="Controller" to="Repository" allowedSites=" local , METHODRETURN " />
		                          </ArchitecturalLevels>
		                          """;

		var config = ParseConfig(configText);

		var edge = config.Graph.DependencyEdges.Should().ContainSingle().Which;
		edge.SiteFilter.AllowedSites.Should().BeEquivalentTo(["Local", "MethodReturn"]);
		edge.SiteFilter.BlockedSites.Should().BeEmpty();
	}

	[Fact]
	public void Parser_ReadsAppliesToDescendants()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                              <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
		                              <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
		                              <AllowedDependency from="Controller" to="Repository" appliesToDescendants="1" />
		                          </ArchitecturalLevels>
		                          """;

		var config = ParseConfig(configText);

		config.Graph.DependencyEdges.Should().ContainSingle()
			.Which.AppliesToDescendants.Should().BeTrue();
	}

	[Fact]
	public void Parser_SkipsAllowedDependency_WhenSiteFilterIsInvalid()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                              <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
		                              <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
		                              <AllowedDependency from="Controller" to="Repository" allowedSites="Constructor" blockedSites="Field" />
		                              <AllowedDependency from="Controller" to="Repository" allowedSites="BananaPeel" />
		                          </ArchitecturalLevels>
		                          """;

		var config = ParseConfig(configText);

		config.Graph.DependencyEdges.Should().BeEmpty();
		config.AllowedEdges.Should().BeEmpty();
	}

	[Fact]
	public void Parser_SkipsDependency_WhenAppliesToDescendantsIsInvalid()
	{
		const string configText = """
		                          <ArchitecturalLevels>
		                              <Layer name="Controller"><Class typeName="PizzaController" /></Layer>
		                              <Layer name="Repository"><Class typeName="CheeseRepository" /></Layer>
		                              <AllowedDependency from="Controller" to="Repository" appliesToDescendants="sometimes" />
		                          </ArchitecturalLevels>
		                          """;

		var config = ParseConfig(configText);

		config.Graph.DependencyEdges.Should().BeEmpty();
		config.AllowedEdges.Should().BeEmpty();
		config.ConfigurationIssues.Should().Contain(item => item.Message.Contains("appliesToDescendants"));
	}

	private static AnalyzerConfiguration ParseConfig(string configText)
    {
        var result = ArchitecturalConfigParser.Parse(
            ImmutableArray.Create<AdditionalText>(
                new TestAdditionalText("Architecture.anl", configText)),
            CancellationToken.None);

		return result;
    }

    private sealed class TestAdditionalText(string path, string content) : AdditionalText
	{
		private readonly SourceText _text = SourceText.From(content);

		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			var result = _text;

			return result;
		}
	}
}
