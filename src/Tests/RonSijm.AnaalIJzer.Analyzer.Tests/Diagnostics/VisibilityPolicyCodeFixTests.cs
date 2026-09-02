using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class VisibilityPolicyCodeFixTests
{
	[Fact]
	public async Task AllowListPolicy_AddsReportedAccessibility()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Class typeName="PublicQueryable" />
			    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal, File" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = "public class PublicQueryable { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.VisibilityPolicyViolation,
			"Allow visibility 'Public' in VisibilityPolicy");

		updatedConfig.Should().Contain("allowedAccessibilities=\"Public, Internal, File\"");
	}

	[Fact]
	public async Task BlockListPolicy_RemovesReportedAccessibility()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Class typeName="PublicQueryable" />
			    <VisibilityPolicy targets="Type" blockedAccessibilities="Public, Internal" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = "public class PublicQueryable { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.VisibilityPolicyViolation,
			"Remove visibility 'Public' from blockedAccessibilities");

		updatedConfig.Should().Contain("blockedAccessibilities=\"Internal\"");
	}

	[Fact]
	public async Task SingleBlockedAccessibility_RemovesWholeVisibilityPolicy()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Class typeName="PublicQueryable" />
			    <VisibilityPolicy targets="Type" blockedAccessibilities="Public" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = "public class PublicQueryable { }";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.VisibilityPolicyViolation,
			"Remove VisibilityPolicy that only blocks 'Public'");

		updatedConfig.Should().NotContain("<VisibilityPolicy");
	}

	[Fact]
	public async Task AllowListPolicy_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Policy">
			    <Class typeName="PublicQueryable" />
			    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal, File" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class PublicQueryable { }
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.VisibilityPolicyViolation,
			"Allow visibility 'Public' in VisibilityPolicy");

		updatedSource.Should().Contain("allowedAccessibilities=\"Public, Internal, File\"");
	}
}
