using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class ApiSurfacePolicyCodeFixTests
{
	[Fact]
	public async Task ApiSurfaceLeakage_AddsAllowedLayer()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <AllowedLayer path="/Contracts" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
			    public LollyQueryable OrderRaw() => new();
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage,
			"Allow API surface to expose '/QuerySurface'");

		updatedConfig.Should().Contain("""<AllowedLayer path="/QuerySurface" />""");
	}

	[Fact]
	public async Task ApiSurfaceLeakage_RepairsExistingAllowedLayerSiteFilter()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <AllowedLayer path="/Contracts" allowedSites="MethodReturn" />
			      <AllowedLayer path="/QuerySurface" allowedSites="Property" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
			    public LollyQueryable OrderRaw() => new();
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage,
			"Add site 'MethodReturn' to ApiSurface AllowedLayer '/QuerySurface'");

		updatedConfig.Should().Contain("allowedSites=\"MethodReturn, Property\"");
	}

	[Fact]
	public async Task ApiSurfaceLeakage_DisablesRequireRecognizedTypes()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface requireRecognizedTypes="true">
			      <AllowedLayer path="/Contracts" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class UnknownType { }
			public class CandyService
			{
			    public UnknownType OrderRaw() => new();
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage,
			"Disable requireRecognizedTypes on ApiSurface");

		updatedConfig.Should().Contain("requireRecognizedTypes=\"false\"");
	}

	[Fact]
	public async Task ApiSurfaceLeakage_RelaxesBlockedLayerAtCurrentSite()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <BlockedLayer path="/QuerySurface" allowedSites="MethodReturn, Property" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
			    public LollyQueryable OrderRaw() => new();
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage,
			"Stop blocking API-surface layer '/QuerySurface' at MethodReturn");

		updatedConfig.Should().Contain("allowedSites=\"Property\"");
	}

	[Fact]
	public async Task ForbiddenTransitiveExposure_AddsAllowedLayer()
	{
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <AllowedLayer path="/Contracts" />
			      <TransitiveExposure />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";
		const string source = """
			public class LollyQueryable { }
			public class LollyProjection
			{
			    public LollyQueryable Secret { get; } = new();
			}
			public class CandyService
			{
			    public LollyProjection OrderProjected() => new();
			}
			""";

		var updatedConfig = await AnalyzerTestHelper.ApplyConfigurationCodeFixAsync(
			source,
			config,
			ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure,
			"Allow API surface to expose '/QuerySurface'");

		updatedConfig.Should().Contain("""<AllowedLayer path="/QuerySurface" />""");
	}

	[Fact]
	public async Task ApiSurfaceLeakage_InlineSettings_UpdatesAssemblyMetadata()
	{
		const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <AllowedLayer path="/Contracts" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""")]

			public class LollyQueryable { }
			public class CandyService
			{
			    public LollyQueryable OrderRaw() => new();
			}
			"""";

		var updatedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(
			source,
			ArchitecturalDiagnosticIds.ApiSurfaceLeakage,
			"Allow API surface to expose '/QuerySurface'");

		updatedSource.Should().Contain("""<AllowedLayer path="/QuerySurface" />""");
	}
}
