using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.ApiSurface;

public sealed class ApiSurfaceAnalyzerTests
{
	[Fact]
	public async Task PublicMethodReturn_ReportsBlockedLayer()
	{
		const string source = """
			public class LollyQueryable { }
			public class LollyProjection { }

			public class CandyService
			{
				public LollyProjection OrderProjected() => new();
				private LollyQueryable BuildQuery() => new();
				public LollyQueryable OrderRaw() => new();
			}
			""";
		var config = CreateBlockConfig();

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDepTypeName].Should().Be("LollyQueryable");
		violation.Properties[ArchitecturalDiagnostics.PropertyDepLayerName].Should().Be("QuerySurface");
		violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.MethodReturn);
		violation.Properties[ArchitecturalDiagnostics.PropertyApiMemberName].Should().Be("CandyService.OrderRaw");
	}

	[Fact]
	public async Task EverySupportedApiSite_Reports()
	{
		const string source = """
			using System;
			using System.Collections.Generic;

			public class QueryBase { }
			public interface IQuerySurface { }
			public sealed class QueryMarkerAttribute : Attribute { }
			public class LollyQueryable { }

			[QueryMarker]
			public class SiteService : QueryBase, IQuerySurface
			{
				public SiteService(LollyQueryable constructorValue) { }
				public LollyQueryable ReturnValue() => new();
				public void Use(LollyQueryable methodValue) { }
				public LollyQueryable Value { get; set; } = new();
				public LollyQueryable Field = new();
				public event Action<LollyQueryable>? Changed;
				public List<LollyQueryable> GenericValue() => new();
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			    <Class startsWith="Query" />
			    <Class endsWith="QuerySurface" />
			    <Class endsWith="QueryMarkerAttribute" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var sites = diagnostics
			.Where(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage)
			.Select(item => item.Properties[ArchitecturalDiagnostics.PropertySite])
			.ToArray();
		sites.Should().Contain(DependencySites.Constructor);
		sites.Should().Contain(DependencySites.Method);
		sites.Should().Contain(DependencySites.MethodReturn);
		sites.Should().Contain(DependencySites.Property);
		sites.Should().Contain(DependencySites.Field);
		sites.Should().Contain(DependencySites.Inheritance);
		sites.Should().Contain(DependencySites.InterfaceImplementation);
		sites.Should().Contain(DependencySites.GenericArgument);
		sites.Should().Contain(DependencySites.Attribute);
	}

	[Fact]
	public async Task ArraysNullableTuplesAndDelegates_AreUnwrapped()
	{
		const string source = """
			#nullable enable
			public struct QueryToken { }
			public class LollyQueryable { }
			public delegate LollyQueryable QueryDelegate(QueryToken token);

			public class ShapeService
			{
				public LollyQueryable[] Array() => [];
				public QueryToken? Nullable() => null;
				public (LollyQueryable Query, int Count) Tuple() => default;
				public QueryDelegate Delegate() => null!;
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class startsWith="Query" />
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Count(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage).Should().BeGreaterThanOrEqualTo(5);
	}

	[Fact]
	public async Task GenericArgumentViolation_ReportsTheArgumentSpan()
	{
		const string source = """
			using System.Collections.Generic;
			public class LollyQueryable { }
			public class CandyService
			{
				public List<LollyQueryable> Order() => new();
			}
			""";
		var config = CreateBlockConfig();

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item =>
			item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage
			&& item.Properties[ArchitecturalDiagnostics.PropertySite] == DependencySites.GenericArgument).Subject;
		source.Substring(violation.Location.SourceSpan.Start, violation.Location.SourceSpan.Length).Should().Be("LollyQueryable");
	}

	[Fact]
	public async Task NonPublicDeclarations_DoNotReport()
	{
		const string source = """
			public class LollyQueryable { }

			public class PublicService
			{
				private LollyQueryable PrivateMethod() => new();
				internal LollyQueryable InternalProperty { get; } = new();
			}

			internal class InternalService
			{
				public LollyQueryable PublicInsideInternal() => new();
			}
			""";
		var config = CreateBlockConfig();

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	[Fact]
	public async Task PublicInterfaceMember_IsChecked()
	{
		const string source = """
			public class LollyQueryable { }

			public interface ICandyService
			{
				LollyQueryable Order();
			}
			""";
		var config = CreateBlockConfig();

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	[Fact]
	public async Task AllowedLayerPermitsType_AndBlockedLayerWins()
	{
		const string source = """
			public class LollyProjection { }
			public class CandyService
			{
				public LollyProjection Order() => new();
			}
			""";
		const string allowedConfig = """
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
			</ArchitecturalLevels>
			""";
		const string blockedConfig = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <AllowedLayer path="/Contracts" />
			      <BlockedLayer path="/Contracts" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var allowedDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, allowedConfig);
		var blockedDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, blockedConfig);

		allowedDiagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
		blockedDiagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	[Fact]
	public async Task SiteFilters_RestrictOnlyApplicableSites()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
				public LollyQueryable Value { get; } = new();
				public LollyQueryable Order() => new();
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <BlockedLayer path="/QuerySurface" allowedSites="MethodReturn" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.MethodReturn);
	}

	[Fact]
	public async Task ParentAndChildPolicies_AreCumulative()
	{
		const string source = """
			namespace Shop.Application
			{
				public class LollyQueryable { }
				public class CandyService
				{
					public LollyQueryable Order() => new();
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ApiSurface>
			      <BlockedLayer path="/QuerySurface" />
			    </ApiSurface>
			    <Layer name="Services">
			      <Class endsWith="Service" />
			      <ApiSurface>
			        <AllowedLayer path="/QuerySurface" />
			      </ApiSurface>
			    </Layer>
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	[Fact]
	public async Task ParentLayerReference_SelectsDescendants()
	{
		const string source = """
			public class LollyProjection { }
			public class CandyService
			{
				public LollyProjection Order() => new();
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <AllowedLayer path="/Contracts" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Namespace startsWith="Example" />
			    <Layer name="Responses">
			      <Class endsWith="Projection" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	[Fact]
	public async Task RequireRecognizedTypes_IsOptIn()
	{
		const string source = """
			public class UnknownType { }
			public class CandyService
			{
				public UnknownType Order() => new();
			}
			""";
		var ignoredConfig = CreateAllowListConfig(false);
		var requiredConfig = CreateAllowListConfig(true);

		var ignoredDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, ignoredConfig);
		var requiredDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, requiredConfig);

		ignoredDiagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
		requiredDiagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	[Theory]
	[InlineData("""<ApiSurface requireRecognizedTypes="perhaps"><AllowedLayer path="/Contracts" /></ApiSurface>""")]
	[InlineData("""<ApiSurface />""")]
	[InlineData("""<ApiSurface><AllowedLayer path="/Unknown" /></ApiSurface>""")]
	[InlineData("""<ApiSurface><AllowedLayer path="/Contracts" allowedSites="Method" blockedSites="Property" /></ApiSurface>""")]
	[InlineData("""<ApiSurface><AllowedLayer path="/Contracts" allowedSites="Unknown" /></ApiSurface>""")]
	public async Task InvalidConfiguration_ReportsArch006(string policy)
	{
		var config = $$"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    {{policy}}
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CandyService { }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task ConfigurationWithoutApiSurface_RemainsUnchanged()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
				public LollyQueryable Order() => new();
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application"><Class endsWith="Service" /></Layer>
			  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
	}

	private static string CreateBlockConfig()
	{
		const string result = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface>
			      <BlockedLayer path="/QuerySurface" />
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

		return result;
	}

	private static string CreateAllowListConfig(bool requireRecognizedTypes)
	{
		var result = $$"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface requireRecognizedTypes="{{requireRecognizedTypes.ToString().ToLowerInvariant()}}">
			      <AllowedLayer path="/Contracts" />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Projection" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		return result;
	}
}
