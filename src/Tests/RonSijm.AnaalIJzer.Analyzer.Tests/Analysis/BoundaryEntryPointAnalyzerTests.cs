using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class BoundaryEntryPointAnalyzerTests
{
	[Fact]
	public async Task ExternalDependency_ThroughConfiguredEntryPoint_Passes()
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
		                          <AllowedDependency from="Implementation" to="Contracts" />
		                          <Layer name="Contracts">
		                            <Class endsWith="Contract" />
		                          </Layer>
		                          <Layer name="Implementation">
		                            <Class endsWith="Service" />
		                          </Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[
				(@"D:\repo\Presentation\CandyController.cs", "using Shop.Ordering.Contracts; public class CandyController(PlaceCandyContract contract) { }"),
				(@"D:\repo\Contracts\PlaceCandyContract.cs", "namespace Shop.Ordering.Contracts; public class PlaceCandyContract { }")
			],
			null,
			("Architecture.anl", config));

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.BoundaryEntryPointViolation);
	}

	[Fact]
	public async Task ExternalDependency_IntoImplementation_ReportsArch016()
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
		                          <AllowedDependency from="Implementation" to="Contracts" />
		                          <Layer name="Contracts">
		                            <Class endsWith="Contract" />
		                          </Layer>
		                          <Layer name="Implementation">
		                            <Class endsWith="Service" />
		                          </Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[
				(@"D:\repo\Presentation\CandyController.cs", "using Shop.Ordering.Implementation; public class CandyController(CandyOrderingService service) { }"),
				(@"D:\repo\Implementation\CandyOrderingService.cs", "namespace Shop.Ordering.Implementation; public class CandyOrderingService { }")
			],
			null,
			("Architecture.anl", config));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.BoundaryEntryPointViolation).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyBoundaryLayerName].Should().Be("Ordering");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyViolationReason].Should().Contain("permits entry only through");
	}

	[Fact]
	public async Task MissingDependencyEdge_StillReportsArch001_NotArch016()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Presentation">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Shop.Ordering" />
		                          <EntryPoints>
		                            <EntryPoint layer="Contracts" />
		                          </EntryPoints>
		                          <Layer name="Contracts">
		                            <Class endsWith="Contract" />
		                          </Layer>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[
				(@"D:\repo\Presentation\CandyController.cs", "using Shop.Ordering.Contracts; public class CandyController(PlaceCandyContract contract) { }"),
				(@"D:\repo\Contracts\PlaceCandyContract.cs", "namespace Shop.Ordering.Contracts; public class PlaceCandyContract { }")
			],
			null,
			("Architecture.anl", config));

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.BoundaryEntryPointViolation);
	}

	[Fact]
	public async Task EntryPointSiteFilter_ReportsSiteReason()
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

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
			[
				(@"D:\repo\Presentation\CandyController.cs", "using Shop.Ordering.Contracts; public class CandyController(PlaceCandyContract contract) { }"),
				(@"D:\repo\Contracts\PlaceCandyContract.cs", "namespace Shop.Ordering.Contracts; public class PlaceCandyContract { }")
			],
			null,
			("Architecture.anl", config));

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.BoundaryEntryPointViolation)
			.Which.Properties[ArchitecturalDiagnostics.PropertyEntryPointFailureReason].Should().Be("the matching entry point does not allow site Constructor");
	}
}
