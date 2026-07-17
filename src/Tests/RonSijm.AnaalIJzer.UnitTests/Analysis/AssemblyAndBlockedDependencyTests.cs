using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Analysis;

public sealed class AssemblyAndBlockedDependencyTests
{
	[Fact]
	public async Task AssemblyMatcher_ClassifiesCaller()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Assembly exactName="TestAssembly" /></Layer>
		                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class TargetDependency { }
		                      public class CallerType(TargetDependency dependency) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task AssemblyMatcher_RespectsAllowedDependency()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Assembly exactName="TestAssembly" /></Layer>
		                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                        <AllowedDependency from="Caller" to="Dependency" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class TargetDependency { }
		                      public class CallerType(TargetDependency dependency) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task BlockedDependency_OverridesWildcardAllowance()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                        <AllowedDependency from="*" to="Dependency" />
		                        <BlockedDependency from="Caller" to="Dependency" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class TargetDependency { }
		                      public class CallerType(TargetDependency dependency) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.GetMessage().Should().Contain("explicitly blocks this dependency at Constructor");
	}

	[Fact]
	public async Task SiteFilteredBlockedDependency_BlocksOnlyConfiguredSites()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                        <AllowedDependency from="*" to="Dependency" />
		                        <BlockedDependency from="Caller" to="Dependency" allowedSites="Constructor" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class TargetDependency { }
		                      public class CallerType(TargetDependency dependency)
		                      {
		                          public TargetDependency Get() => dependency;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Constructor);
	}
}
