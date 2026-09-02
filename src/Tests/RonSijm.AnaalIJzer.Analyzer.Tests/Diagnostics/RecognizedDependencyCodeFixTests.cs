using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class RecognizedDependencyCodeFixTests
{
	[Fact]
	public async Task ClassifyUnknownDependency_AddsClassMatcherToSelectedLayer()
	{
		const string config = """
			<ArchitecturalLevels requireRecognizedDependencies="Constructor">
			  <Layer name="Caller">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			  <Layer name="Mystery">
			    <Class typeName="KnownMystery" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class MysteryIngredient { }
			public class KnownMystery { }
			public class PizzaWaiter(MysteryIngredient ingredient) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.UnrecognizedDependency,
			"Classify 'MysteryIngredient' into layer 'Mystery'");

		updatedConfig.Should().Contain("<Class typeName=\"MysteryIngredient\" />");
	}

	[Fact]
	public async Task StopRequiringRecognizedDependencyGlobally_RemovesCurrentSite()
	{
		const string config = """
			<ArchitecturalLevels requireRecognizedDependencies="Constructor, Local">
			  <Layer name="Caller">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class MysteryIngredient { }
			public class PizzaWaiter(MysteryIngredient ingredient) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.UnrecognizedDependency,
			"Stop requiring recognized dependencies at Constructor globally");

		updatedConfig.Should().Contain("requireRecognizedDependencies=\"Local\"");
		updatedConfig.Should().NotContain("requireRecognizedDependencies=\"Constructor, Local\"");
	}

	[Fact]
	public async Task StopRequiringRecognizedDependencyForLayer_RemovesCurrentSite()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Caller" requireRecognizedDependencies="Constructor, Local">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class MysteryIngredient { }
			public class PizzaWaiter(MysteryIngredient ingredient) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.UnrecognizedDependency,
			"Stop requiring recognized dependencies at Constructor for layer 'Caller'");

		updatedConfig.Should().Contain("requireRecognizedDependencies=\"Local\"");
	}

	[Fact]
	public async Task ClassifyUnknownDependency_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels requireRecognizedDependencies="Constructor">
			  <Layer name="Caller">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			  <Layer name="Mystery">
			    <Class typeName="KnownMystery" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class MysteryIngredient { }
			public class KnownMystery { }
			public class PizzaWaiter(MysteryIngredient ingredient) { }
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.UnrecognizedDependency,
			"Classify 'MysteryIngredient' into layer 'Mystery'");

		updatedSource.Should().Contain("<Class typeName=\"MysteryIngredient\" />");
	}
}
