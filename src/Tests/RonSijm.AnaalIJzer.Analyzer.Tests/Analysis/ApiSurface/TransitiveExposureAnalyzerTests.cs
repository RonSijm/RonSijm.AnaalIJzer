using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.ApiSurface;

public sealed class TransitiveExposureAnalyzerTests
{
	[Fact]
	public async Task PublicContractProperty_ReportsShortestTransitivePath()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyExposureDepth].Should().Be("1");
		violation.Properties[ArchitecturalDiagnostics.PropertyExposurePath].Should().Contain("CandyService.Order");
		violation.Properties[ArchitecturalDiagnostics.PropertyExposurePath].Should().Contain("CandyReceipt.RawQuery");
		violation.Properties[ArchitecturalDiagnostics.PropertyExposurePath].Should().EndWith("LollyQueryable");
		violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(DependencySites.Property);
		violation.AdditionalLocations.Should().ContainSingle();
	}

	[Fact]
	public async Task DirectForbiddenType_ReportsArch009Only()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyService
			{
				public LollyQueryable Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ApiSurfaceLeakage);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
	}

	[Fact]
	public async Task TwoLevelObjectGraph_RespectsMaximumDepth()
	{
		const string source = """
			public class LollyQueryable { }
			public class ReceiptDetails
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyReceipt
			{
				public ReceiptDetails Details { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var shallowDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(1));
		var deepDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(2));

		shallowDiagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
		var violation = deepDiagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyExposureDepth].Should().Be("2");
	}

	[Fact]
	public async Task RecursiveContract_DoesNotLoop()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public CandyReceipt? Next { get; set; }
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(10));

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
	}

	[Fact]
	public async Task PrivateNestedMember_IsIgnored()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				private LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
	}

	[Fact]
	public async Task MissingTransitiveExposureOption_KeepsDirectOnlyBehavior()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(includeTransitive: false));

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
	}

	[Fact]
	public async Task NestedMemberSiteFilter_UsesNestedSite()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var methodOnlyDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(blockedRuleAttributes: "allowedSites=\"Method\"", includeAllowedLayer: false));
		var propertyDiagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(blockedRuleAttributes: "allowedSites=\"Property\"", includeAllowedLayer: false));

		methodOnlyDiagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
		propertyDiagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure);
	}

	[Fact]
	public async Task RequiredRecognition_AppliesToNestedTypes()
	{
		const string source = """
			public class MysteryIngredient { }
			public class CandyReceipt
			{
				public MysteryIngredient Ingredient { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(requireRecognizedTypes: true));

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDepLayerName].Should().Be("unrecognized");
	}

	[Fact]
	public async Task GenericArgumentsAndArrays_AreTraversed()
	{
		const string source = """
			using System.Collections.Generic;

			public class LollyQueryable { }
			public class CandyReceipt
			{
				public List<LollyQueryable[]> Queries { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyExposurePath].Should().Contain("CandyReceipt.Queries");
		violation.Properties[ArchitecturalDiagnostics.PropertyExposurePath].Should().EndWith("LollyQueryable");
	}

	[Fact]
	public async Task TwoTypeCycle_DoesNotLoopAndFindsShortestPath()
	{
		const string source = """
			public class LollyQueryable { }
			public class ReceiptDetails
			{
				public CandyReceipt Receipt { get; } = new();
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyReceipt
			{
				public ReceiptDetails Details { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt Order() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(10));

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyExposureDepth].Should().Be("2");
		violation.Properties[ArchitecturalDiagnostics.PropertyExposurePath].Should().Contain("ReceiptDetails.RawQuery");
	}

	[Fact]
	public async Task SeparateRootMembers_ReportSeparately()
	{
		const string source = """
			public class LollyQueryable { }
			public class CandyReceipt
			{
				public LollyQueryable RawQuery { get; } = new();
			}
			public class CandyService
			{
				public CandyReceipt OrderFirst() => new();
				public CandyReceipt OrderSecond() => new();
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

		diagnostics.Count(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenTransitiveExposure).Should().Be(2);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("11")]
	[InlineData("many")]
	public async Task InvalidMaximumDepth_ReportsConfigurationIssue(string value)
	{
		var config = CreateConfig(transitiveValue: value);

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class CandyService { }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	private static string CreateConfig(
		int maxDepth = 3,
		bool includeTransitive = true,
		string? transitiveValue = null,
		string? blockedRuleAttributes = null,
		bool requireRecognizedTypes = false,
		bool includeAllowedLayer = true)
	{
		var transitive = includeTransitive
			? $"<TransitiveExposure maxDepth=\"{transitiveValue ?? maxDepth.ToString(CultureInfo.InvariantCulture)}\" />"
			: string.Empty;
		var recognition = requireRecognizedTypes ? " requireRecognizedTypes=\"true\"" : string.Empty;
		var blockedAttributes = string.IsNullOrWhiteSpace(blockedRuleAttributes) ? string.Empty : " " + blockedRuleAttributes;
		var allowedLayer = includeAllowedLayer ? """<AllowedLayer path="/Contracts" />""" : string.Empty;
		var result = $$"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    <ApiSurface{{recognition}}>
			      {{transitive}}
			      {{allowedLayer}}
			      <BlockedLayer path="/QuerySurface"{{blockedAttributes}} />
			    </ApiSurface>
			  </Layer>
			  <Layer name="Contracts">
			    <Class endsWith="Receipt" />
			    <Class endsWith="Details" />
			  </Layer>
			  <Layer name="QuerySurface">
			    <Class endsWith="Queryable" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		return result;
	}
}
