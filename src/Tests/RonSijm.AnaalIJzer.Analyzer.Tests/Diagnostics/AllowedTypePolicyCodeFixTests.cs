using System.Xml.Linq;
using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class AllowedTypePolicyCodeFixTests
{
	[Fact]
	public async Task LayerScopedAllowedListFailure_AddsExactTypeMatcher()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Caller">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			  <Layer name="Ingredients">
			    <Class typeName="CheeseShelf" />
			    <Allowed>
			      <Class typeName="PizzaChef" />
			    </Allowed>
			  </Layer>
			  <AllowedDependency from="Caller" to="Ingredients" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public class PizzaChef { }
			public class CheeseShelf { }
			public class PizzaWaiter(CheeseShelf shelf) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ForbiddenDependency,
			"Allow 'CheeseShelf' in applicable <Allowed> lists");

		updatedConfig.Should().Contain("<Class typeName=\"CheeseShelf\" />");
	}

	[Fact]
	public async Task GlobalAndLayerAllowedListFailure_UpdatesEveryApplicableList()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Allowed>
			    <Class typeName="PizzaChef" />
			  </Allowed>
			  <Layer name="Caller">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			  <Layer name="Ingredients">
			    <Class typeName="CheeseShelf" />
			    <Allowed>
			      <Class typeName="PizzaChef" />
			    </Allowed>
			  </Layer>
			  <AllowedDependency from="Caller" to="Ingredients" />
			</ArchitecturalLevels>
			""";
		const string source = """
			public class PizzaChef { }
			public class CheeseShelf { }
			public class PizzaWaiter(CheeseShelf shelf) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ForbiddenDependency,
			"Allow 'CheeseShelf' in applicable <Allowed> lists");

		var document = XDocument.Parse(updatedConfig);
		var allowedOccurrences = document
			.Descendants("Allowed")
			.Elements("Class")
			.Count(element => string.Equals(element.Attribute("typeName")?.Value, "CheeseShelf", StringComparison.Ordinal));

		allowedOccurrences.Should().Be(2);
	}

	[Fact]
	public async Task AllowedListFailure_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Caller">
			    <Class typeName="PizzaWaiter" />
			  </Layer>
			  <Layer name="Ingredients">
			    <Class typeName="CheeseShelf" />
			    <Allowed>
			      <Class typeName="PizzaChef" />
			    </Allowed>
			  </Layer>
			  <AllowedDependency from="Caller" to="Ingredients" />
			</ArchitecturalLevels>
			""")]

			public class PizzaChef { }
			public class CheeseShelf { }
			public class PizzaWaiter(CheeseShelf shelf) { }
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.ForbiddenDependency,
			"Allow 'CheeseShelf' in applicable <Allowed> lists");

		updatedSource.Should().Contain("<Class typeName=\"CheeseShelf\" />");
	}
}
