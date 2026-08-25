using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class ObservedDependencyCycleAnalyzerTests
{
	[Fact]
	public async Task ObservedCycle_WhenDisabled_DoesNotReportArch018()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Shop.Ordering" />
		                        </Layer>
		                        <Layer name="Notifications">
		                          <Namespace startsWith="Shop.Notifications" />
		                        </Layer>
		                        <AllowedDependency from="Ordering" to="Notifications" />
		                        <AllowedDependency from="Notifications" to="Ordering" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetObservedCycleSource(), config);

		diagnostics.Should().NotContain(diagnostic => diagnostic.Id == ArchitecturalDiagnosticIds.ObservedDependencyCycle);
	}

	[Fact]
	public async Task ObservedCycle_WhenEnabled_ReportsArch018()
	{
		const string config = """
		                      <ArchitecturalLevels enforceObservedAcyclic="true">
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Shop.Ordering" />
		                        </Layer>
		                        <Layer name="Notifications">
		                          <Namespace startsWith="Shop.Notifications" />
		                        </Layer>
		                        <AllowedDependency from="Ordering" to="Notifications" />
		                        <AllowedDependency from="Notifications" to="Ordering" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetObservedCycleSource(), config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ObservedDependencyCycle).Subject;
		diagnostic.GetMessage().Should().Contain("Notifications -> Ordering -> Notifications");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyCycleLayers].Should().Be("Notifications|Ordering");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyCycleLength].Should().Be("2");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyObservedSites].Should().Be("Constructor");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyCycleScope].Should().Be("Project");
	}

	[Fact]
	public async Task ObservedCycle_InvalidBoolean_ReportsArch006()
	{
		const string config = """
		                      <ArchitecturalLevels enforceObservedAcyclic="maybe">
		                        <Layer name="Ordering">
		                          <Namespace startsWith="Shop.Ordering" />
		                        </Layer>
		                        <Layer name="Notifications">
		                          <Namespace startsWith="Shop.Notifications" />
		                        </Layer>
		                        <AllowedDependency from="Ordering" to="Notifications" />
		                        <AllowedDependency from="Notifications" to="Ordering" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetObservedCycleSource(), config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration && item.GetMessage().Contains("enforceObservedAcyclic", StringComparison.Ordinal));
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ObservedDependencyCycle);
	}

	private static string GetObservedCycleSource()
	{
		var result = """
		             namespace Shop.Ordering
		             {
		                 using Shop.Notifications;

		                 public sealed class OrderService
		                 {
		                     public OrderService(NotificationPublisher notifications) { }
		                 }
		             }

		             namespace Shop.Notifications
		             {
		                 using Shop.Ordering;

		                 public sealed class NotificationPublisher
		                 {
		                     public NotificationPublisher(OrderService orders) { }
		                 }
		             }
		             """;

		return result;
	}
}
