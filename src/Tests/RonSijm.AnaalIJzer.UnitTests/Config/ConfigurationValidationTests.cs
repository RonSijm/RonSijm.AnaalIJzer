using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Config;

public sealed class ConfigurationValidationTests
{
	[Fact]
	public async Task MalformedXml_ReportsARCH006()
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CallerType { }", "<ArchitecturalLevels><Layer>");

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task UnknownAllowedDependencyLayer_ReportsARCH006()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <AllowedDependency from="Caller" to="Missing" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CallerType { }", config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task DuplicateLayer_ReportsARCH006()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Caller"><Class typeName="OtherType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CallerType { }", config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task InvalidRegex_ReportsARCH006()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class regex="[" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CallerType { }", config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task MultipleMatcherAttributes_AreCombined()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class startsWith="Caller" endsWith="Type" typeKind="Class" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CallerType { }", config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task MissingInclude_ReportsARCH006()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Include path="Missing.xml" />
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CallerType { }", config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task AcyclicEnforcement_ReportsARCH007()
	{
		const string config = """
		                      <ArchitecturalLevels enforceAcyclic="true">
		                        <Layer name="A"><Class typeName="AType" /></Layer>
		                        <Layer name="B"><Class typeName="BType" /></Layer>
		                        <Layer name="C"><Class typeName="CType" /></Layer>
		                        <AllowedDependency from="A" to="B" />
		                        <AllowedDependency from="B" to="C" />
		                        <AllowedDependency from="C" to="A" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class AType { } public class BType { } public class CType { }", config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.CyclicDependencyGraph).Subject;
		diagnostic.GetMessage().Should().Contain("A -> B -> C -> A");
	}

	[Fact]
	public async Task Cycle_IsAllowedWhenEnforcementIsDisabled()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="A"><Class typeName="AType" /></Layer>
		                        <Layer name="B"><Class typeName="BType" /></Layer>
		                        <AllowedDependency from="A" to="B" />
		                        <AllowedDependency from="B" to="A" />
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class AType { } public class BType { }", config);

		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData("B", "A")]
	[InlineData("*", "A")]
	public async Task UnfilteredBlockedEdge_BreaksConfiguredCycle(string blockedFrom, string blockedTo)
	{
		var config = $$"""
		               <ArchitecturalLevels enforceAcyclic="true">
		                 <Layer name="A"><Class typeName="AType" /></Layer>
		                 <Layer name="B"><Class typeName="BType" /></Layer>
		                 <AllowedDependency from="A" to="B" />
		                 <AllowedDependency from="B" to="A" />
		                 <BlockedDependency from="{{blockedFrom}}" to="{{blockedTo}}" />
		               </ArchitecturalLevels>
		               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class AType { } public class BType { }", config);

		diagnostics.Should().BeEmpty();
	}
}
