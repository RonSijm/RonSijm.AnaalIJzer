namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class StructuralDependencyTests
{
	public static TheoryData<string> StructuralSites => new()
	{
		DependencySites.Inheritance,
		DependencySites.Attribute,
		DependencySites.StaticMember
	};

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH001(string site)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), GetConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH003(string site)
	{
		var config = """
		             <ArchitecturalLevels>
		               <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		               <Forbidden><Class typeName="TargetDependency" /></Forbidden>
		             </ArchitecturalLevels>
		             """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH004(string site)
	{
		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(GetSource(site), GetConfig("<AllowedDependency from=\"Dependency\" to=\"Caller\" />"));

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
	}

	[Theory]
	[MemberData(nameof(StructuralSites))]
	public async Task StructuralSite_ReportsARCH005(string site)
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

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.SameLayerDependency).Subject;
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

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
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

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task RecordPrimaryConstructor_IsAnalyzed()
	{
		const string source = """
		                      public class TargetDependency { }
		                      public record CallerType(TargetDependency Dependency);
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, GetConfig());

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Subject;
		diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Constructor);
	}

	private static string GetConfig(string edge = "") =>
		$$"""
		  <ArchitecturalLevels>
		    <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		    <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		    {{edge}}
		  </ArchitecturalLevels>
		  """;

	private static string GetSource(string site) => site switch
	{
		DependencySites.Inheritance => """
		                               public class TargetDependency { }
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
}
