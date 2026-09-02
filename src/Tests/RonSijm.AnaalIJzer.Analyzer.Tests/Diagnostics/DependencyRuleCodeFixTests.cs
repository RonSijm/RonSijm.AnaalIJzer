using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class DependencyRuleCodeFixTests
{
	[Fact]
	public async Task MissingAllowedDependency_AddsAllowedDependencyToConfiguration()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Waiter">
			    <Class endsWith="Waiter" />
			  </Layer>
			  <Layer name="Chef">
			    <Class endsWith="Chef" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public sealed class PizzaChef { }
			public sealed class OrderWaiter(PizzaChef chef) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.IllegalLevelDependency,
			"Add allowed dependency 'Waiter' -> 'Chef'");

		updatedConfig.Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Chef\" />");
	}

	[Fact]
	public async Task AllowedSitesViolation_AppendsCurrentSiteToAllowedSites()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Caller">
			    <Class typeName="AllowedLocalSiteExample" />
			  </Layer>
			  <Layer name="AllowedLocalDependency">
			    <Class typeName="AllowedLocalSweet" />
			  </Layer>
			  <AllowedDependency from="Caller" to="AllowedLocalDependency" allowedSites="Constructor" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public sealed class AllowedLocalSweet { }
			public sealed class AllowedLocalSiteExample
			{
			    public void Run()
			    {
			        AllowedLocalSweet sweet = null!;
			        _ = sweet;
			    }
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.IllegalLevelDependency,
			"Add site 'Local' to allowedSites");

		updatedConfig.Should().Contain("allowedSites=\"Constructor, Local\"");
	}

	[Fact]
	public async Task BlockedSitesViolation_RemovesCurrentSiteFromBlockedSites()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Caller">
			    <Class typeName="BlockedFieldSiteExample" />
			  </Layer>
			  <Layer name="BlockedFieldDependency">
			    <Class typeName="BlockedFieldSweet" />
			  </Layer>
			  <AllowedDependency from="Caller" to="BlockedFieldDependency" blockedSites="Field, Property" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public sealed class BlockedFieldSweet { }
			public sealed class BlockedFieldSiteExample
			{
			    private readonly BlockedFieldSweet sweet = null!;
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.IllegalLevelDependency,
			"Remove site 'Field' from blockedSites");

		updatedConfig.Should().Contain("blockedSites=\"Property\"");
		updatedConfig.Should().NotContain("blockedSites=\"Field, Property\"");
	}

	[Fact]
	public async Task SameLayerDependency_OffersSiteLimitedSelfEdge()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="DataAbstraction">
			    <Class endsWith="Repository" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public interface IExampleRepository { }
			public class ExampleRepository : IExampleRepository { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.SameLayerDependency,
			"Allow same-layer dependency 'DataAbstraction' -> 'DataAbstraction' at InterfaceImplementation");

		updatedConfig.Should().Contain("<AllowedDependency from=\"DataAbstraction\" to=\"DataAbstraction\" allowedSites=\"InterfaceImplementation\" />");
	}

	[Fact]
	public async Task MissingAllowedDependency_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Waiter">
			    <Class endsWith="Waiter" />
			  </Layer>
			  <Layer name="Chef">
			    <Class endsWith="Chef" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public sealed class PizzaChef { }
			public sealed class OrderWaiter(PizzaChef chef) { }
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.IllegalLevelDependency,
			"Add allowed dependency 'Waiter' -> 'Chef'");

		updatedSource.Should().Contain("<AllowedDependency from=\"Waiter\" to=\"Chef\" />");
	}

	[Fact]
	public async Task WrongDirectionDependency_AddsForwardAllowedDependency()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Controller">
			    <Class endsWith="Controller" />
			  </Layer>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			  </Layer>
			  <AllowedDependency from="Application" to="Controller" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public sealed class PizzaKitchen { }
			public sealed class PizzaController(PizzaKitchen kitchen) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.WrongDirectionDependency,
			"Add allowed dependency 'Controller' -> 'Application'");

		updatedConfig.Should().Contain("<AllowedDependency from=\"Controller\" to=\"Application\" />");
	}

	[Fact]
	public async Task WrongDirectionDependency_OffersFlipConfiguredReverseDependency()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Controller">
			    <Class endsWith="Controller" />
			  </Layer>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			  </Layer>
			  <AllowedDependency from="Application" to="Controller" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public sealed class PizzaKitchen { }
			public sealed class PizzaController(PizzaKitchen kitchen) { }
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.WrongDirectionDependency);

		titles.Should().Contain("Add allowed dependency 'Controller' -> 'Application'");
		titles.Should().Contain("Flip configured dependency 'Application' -> 'Controller' to 'Controller' -> 'Application'");
	}

	[Fact]
	public async Task WrongDirectionDependency_FlipConfiguredReverseDependency_UpdatesExistingRule()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Controller">
			    <Class endsWith="Controller" />
			  </Layer>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			  </Layer>
			  <AllowedDependency from="Application" to="Controller" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public sealed class PizzaKitchen { }
			public sealed class PizzaController(PizzaKitchen kitchen) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.WrongDirectionDependency,
			"Flip configured dependency 'Application' -> 'Controller' to 'Controller' -> 'Application'");

		updatedConfig.Should().Contain("<AllowedDependency from=\"Controller\" to=\"Application\" />");
		updatedConfig.Should().NotContain("<AllowedDependency from=\"Application\" to=\"Controller\" />");
	}
}
