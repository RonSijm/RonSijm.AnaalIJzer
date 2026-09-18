using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

public sealed class StructuralDependencyTests
{
	public static TheoryData<string> StructuralSites =>
	[
		DependencySites.Inheritance,
		DependencySites.InterfaceImplementation,
		DependencySites.Attribute,
		DependencySites.StaticMember
	];

    [Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH_DEP_001(string site)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), GetConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH_TYPE_001(string site)
	{
		var config = """
		             <ArchitecturalLevels>
		               <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		               <Forbidden><Class typeName="TargetDependency" /></Forbidden>
		             </ArchitecturalLevels>
		             """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.TypeNotAllowed).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH_DEP_004(string site)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), GetConfig("<AllowedDependency from=\"Dependency\" to=\"Caller\" />"));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyReverseDirection).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH_DEP_005(string site)
	{
		var config = """
		             <ArchitecturalLevels>
		               <Layer name="Shared">
		                 <Class typeName="CallerType" />
		                 <Class typeName="TargetDependency" />
		               </Layer>
		             </ArchitecturalLevels>
		             """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyPeerScope).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_RespectsAllowedSites(string site)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), GetConfig($"<AllowedDependency from=\"Caller\" to=\"Dependency\" allowedSites=\"{site}\" />"));

		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_RespectsBlockedSites(string site)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), GetConfig($"<AllowedDependency from=\"Caller\" to=\"Dependency\" blockedSites=\"{site}\" />"));

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
	}

	[Theory]
	[InlineData("record CallerType")]
	[InlineData("struct CallerType")]
	[InlineData("interface CallerType")]
	public async Task NonClassType_IsAnalyzedAsCaller(string declaration)
	{
		var source = $$"""
		               public class TargetDependency { }
		               public {{declaration}}
		               {
		                   TargetDependency Get();
		               }
		               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, GetConfig());

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
	}

	[Fact]
	public async Task RecordPrimaryConstructor_IsAnalyzed()
	{
		const string source = """
		                      public class TargetDependency { }
		                      public record CallerType(TargetDependency Dependency);
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, GetConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Constructor);
	}

	[Fact]
	public async Task InterfaceExtendingInterface_IsReportedAsInheritance()
	{
		const string source = """
		                      public interface TargetDependency { }
		                      public interface CallerType : TargetDependency { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, GetConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Inheritance);
	}

	private static string GetConfig(string edge = "")
	{
		var result = $$"""
		               <ArchitecturalLevels>
		                 <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                 <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                 {{edge}}
		               </ArchitecturalLevels>
		               """;

		return result;
	}

	private static string GetSource(string site)
	{
		var result = site switch
		{
			DependencySites.Inheritance => """
			                               public class TargetDependency { }
			                               public class CallerType : TargetDependency { }
			                               """,
			DependencySites.InterfaceImplementation => """
			                                           public interface TargetDependency { }
			                                           public class CallerType : TargetDependency { }
			                                           """,
			DependencySites.Attribute => """
			                             using System;
			                             public class TargetDependency : Attribute { }
			                             [TargetDependency]
			                             public class CallerType { }
			                             """,
			DependencySites.StaticMember => """
			                                public static class TargetDependency
			                                {
			                                    public static void Use() { }
			                                    public static int Value => 42;
			                                }
			                                public class CallerType
			                                {
			                                    public void Run() => TargetDependency.Use();
			                                }
			                                """,
			_ => throw new ArgumentOutOfRangeException(nameof(site), site, null)
		};

		return result;
	}
}
