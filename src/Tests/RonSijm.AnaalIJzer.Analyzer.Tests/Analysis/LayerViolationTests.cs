using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class LayerViolationTests
{
    // ---- Same-layer violations ----

    [Fact]
    public async Task SameLayerDependency_ControllerToController_ReportsARCH_DEP_005()
    {
        const string source = """
		                      public class OtherController { }
		                      public class PatientController(OtherController other) { }
		                      """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

        diagnostics
            .Where(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope)
            .Should().NotBeEmpty();
    }

    [Fact]
    public async Task SameLayerDependency_ManagerToManager_ReportsARCH_DEP_005()
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
            .Where(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope)
            .Should().NotBeEmpty();
    }

    // ---- Wrong-direction violations ----

    [Fact]
    public async Task WrongDirection_ManagerToController_ReportsARCH_DEP_004()
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
            .Where(d => d.Id == ArchitecturalDiagnosticIds.DependencyReverseDirection)
            .Should().NotBeEmpty();
    }

    [Fact]
    public async Task WrongDirection_RepositoryToManager_ReportsARCH_DEP_004()
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
            .Where(d => d.Id == ArchitecturalDiagnosticIds.DependencyReverseDirection)
            .Should().NotBeEmpty();
    }

    // ---- Missing edge ----

    [Fact]
    public async Task MissingEdge_ControllerToRepository_ReportsARCH_DEP_001()
    {
        const string source = """
		                      public class PatientRepository { }
		                      public class PatientController(PatientRepository repo) { }
		                      """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

        diagnostics
            .Where(d => d.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed)
            .Should().NotBeEmpty();
    }

    // ---- Reason classification (ARCH_DEP_001 vs ARCH_DEP_004 vs ARCH_DEP_005) ----

    [Fact]
    public async Task NoEdge_ReportsOnlyARCH_DEP_001_NotARCH_DEP_004OrARCH_DEP_005()
    {
        const string source = """
		                      public class PatientRepository { }
		                      public class PatientController(PatientRepository repo) { }
		                      """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

        diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
        diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.DependencyReverseDirection);
        diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope);
    }

    [Fact]
    public async Task WrongDirection_ReportsOnlyARCH_DEP_004_NotARCH_DEP_001OrARCH_DEP_005()
    {
        const string source = """
		                      public class PatientController { }
		                      public class PatientManager(PatientController controller) { }
		                      """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

        diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.DependencyReverseDirection);
        diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
        diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope);
    }

    [Fact]
    public async Task SameLayer_ReportsOnlyARCH_DEP_005_NotARCH_DEP_001OrARCH_DEP_004()
    {
        const string source = """
		                      public class OtherManager { }
		                      public class PatientManager(OtherManager other) { }
		                      """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

        diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope);
        diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
        diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.DependencyReverseDirection);
    }

    // ---- Explicit self-edge opts in to same-layer dependencies ----

    [Fact]
    public async Task SameLayer_WithExplicitSelfEdge_DoesNotReportARCH_DEP_005()
    {
        // Same Application -> Application dependency that would normally trip ARCH_DEP_005,
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

        var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope).Which;
        diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Constructor);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("ReportingRepository");
    }

    [Fact]
    public async Task SameLayer_SelfEdgeForOneLayer_DoesNotAffectOtherLayers()
    {
        // Application has a self-edge; Controller does not. Controller -> Controller still trips ARCH_DEP_005.
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
            .Where(d => d.Id == ArchitecturalDiagnosticIds.DependencyPeerScope)
            .ToList();

        sameLayer.Should().ContainSingle();
        sameLayer[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("PatientController");
    }
}