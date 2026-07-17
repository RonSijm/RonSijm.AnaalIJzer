using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Analysis;

public sealed class LayerViolationTests
{
	// ---- Same-layer violations ----

	[Fact]
	public async Task SameLayerDependency_ControllerToController_ReportsARCH005()
	{
		const string source = """
		                      public class OtherController { }
		                      public class PatientController(OtherController other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task SameLayerDependency_ManagerToManager_ReportsARCH005()
	{
		const string source = """
		                      public class OtherManager { }
		                      public class PatientManager
		                      {
		                          public PatientManager(OtherManager other) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.Should().NotBeEmpty();
	}

	// ---- Wrong-direction violations ----

	[Fact]
	public async Task WrongDirection_ManagerToController_ReportsARCH004()
	{
		const string source = """
		                      public class PatientController { }
		                      public class PatientManager
		                      {
		                          public PatientManager(PatientController controller) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task WrongDirection_RepositoryToManager_ReportsARCH004()
	{
		const string source = """
		                      public class PatientManager { }
		                      public class PatientRepository
		                      {
		                          public PatientRepository(PatientManager manager) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency)
			.Should().NotBeEmpty();
	}

	// ---- Missing edge ----

	[Fact]
	public async Task MissingEdge_ControllerToRepository_ReportsARCH001()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientController(PatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	// ---- Reason classification (ARCH001 vs ARCH004 vs ARCH005) ----

	[Fact]
	public async Task NoEdge_ReportsOnlyARCH001_NotARCH004OrARCH005()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientController(PatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency);
		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency);
	}

	[Fact]
	public async Task WrongDirection_ReportsOnlyARCH004_NotARCH001OrARCH005()
	{
		const string source = """
		                      public class PatientController { }
		                      public class PatientManager(PatientController controller) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency);
		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency);
	}

	[Fact]
	public async Task SameLayer_ReportsOnlyARCH005_NotARCH001OrARCH004()
	{
		const string source = """
		                      public class OtherManager { }
		                      public class PatientManager(OtherManager other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency);
		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency);
	}

	// ---- Explicit self-edge opts in to same-layer dependencies ----

	[Fact]
	public async Task SameLayer_WithExplicitSelfEdge_DoesNotReportARCH005()
	{
		// Same Application -> Application dependency that would normally trip ARCH005,
		// but the explicit self-edge opts in.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Application" />
		                          <AllowedDependency from="Application" to="Repository" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class OtherManager { }
		                      public class PatientManager(OtherManager other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task SameLayer_InterfaceImplementationOnlySelfEdge_AllowsInterfaceImplementationButNotConstructor()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="DataAbstraction">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="DataAbstraction" to="DataAbstraction" allowedSites="InterfaceImplementation" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IExampleRepository { }
		                      public class ExampleRepository : IExampleRepository { }
		                      public class ReportingRepository(IExampleRepository repository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency).Which;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Constructor);
		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("ReportingRepository");
	}

	[Fact]
	public async Task SameLayer_SelfEdgeForOneLayer_DoesNotAffectOtherLayers()
	{
		// Application has a self-edge; Controller does not. Controller -> Controller still trips ARCH005.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller">
		                              <Class endsWith="Controller" />
		                          </Layer>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Application" />
		                          <AllowedDependency from="Controller" to="Application" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class OtherController { }
		                      public class PatientController(OtherController other) { }
		                      public class OtherManager { }
		                      public class PatientManager(OtherManager other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var sameLayer = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.ToList();

		sameLayer.Should().ContainSingle();
		sameLayer[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("PatientController");
	}
}
