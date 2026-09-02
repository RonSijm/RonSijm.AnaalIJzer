using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class BoundaryEntryPointCodeFixTests
{
	[Fact]
	public async Task BoundaryEntryPointViolation_AddsLayerEntryPoint()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Presentation">
			    <Class endsWith="Controller" />
			  </Layer>
			  <AllowedDependency from="Presentation" to="Ordering" appliesToDescendants="true" />
			  <Layer name="Ordering">
			    <Namespace startsWith="Shop.Ordering" />
			    <EntryPoints>
			      <EntryPoint layer="Contracts" />
			    </EntryPoints>
			    <Layer name="Contracts">
			      <Class endsWith="Contract" />
			    </Layer>
			    <Layer name="Implementation">
			      <Class endsWith="Service" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			namespace Shop.Ordering.Implementation
			{
			    public class CandyOrderingService { }
			}

			public class CandyController(Shop.Ordering.Implementation.CandyOrderingService service) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.BoundaryEntryPointViolation,
			"Add entry point 'Implementation' to boundary 'Ordering'");

		updatedConfig.Should().Contain("""<EntryPoint layer="Implementation" />""");
	}

	[Fact]
	public async Task BoundaryEntryPointViolation_RemovesBlockedSite()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Presentation">
			    <Class endsWith="Controller" />
			  </Layer>
			  <AllowedDependency from="Presentation" to="Ordering" appliesToDescendants="true" />
			  <Layer name="Ordering">
			    <Namespace startsWith="Shop.Ordering" />
			    <EntryPoints>
			      <EntryPoint layer="Contracts" blockedSites="Constructor" />
			    </EntryPoints>
			    <Layer name="Contracts">
			      <Class endsWith="Contract" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			namespace Shop.Ordering.Contracts
			{
			    public class PlaceCandyContract { }
			}

			public class CandyController(Shop.Ordering.Contracts.PlaceCandyContract contract) { }
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.BoundaryEntryPointViolation,
			"Remove site 'Constructor' from blocked entry point for 'Ordering/Contracts'");

		updatedConfig.Should().NotContain("blockedSites=\"Constructor\"");
	}

	[Fact]
	public async Task BoundaryEntryPointViolation_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Presentation">
			    <Class endsWith="Controller" />
			  </Layer>
			  <AllowedDependency from="Presentation" to="Ordering" appliesToDescendants="true" />
			  <Layer name="Ordering">
			    <Namespace startsWith="Shop.Ordering" />
			    <EntryPoints>
			      <EntryPoint layer="Contracts" />
			    </EntryPoints>
			    <Layer name="Contracts">
			      <Class endsWith="Contract" />
			    </Layer>
			    <Layer name="Implementation">
			      <Class endsWith="Service" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""")]

			namespace Shop.Ordering.Implementation
			{
			    public class CandyOrderingService { }
			}

			public class CandyController(Shop.Ordering.Implementation.CandyOrderingService service) { }
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.BoundaryEntryPointViolation,
			"Add entry point 'Implementation' to boundary 'Ordering'");

		updatedSource.Should().Contain("""<EntryPoint layer="Implementation" />""");
	}
}
