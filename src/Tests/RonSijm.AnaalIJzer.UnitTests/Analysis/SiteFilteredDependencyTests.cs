using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Analysis;

public sealed class SiteFilteredDependencyTests
{
	public static IEnumerable<object[]> AllSitesAndLayeringDiagnostics()
	{
		foreach (var site in AllSites())
		foreach (var diagnosticId in new[]
		         {
			         ArchitecturalDiagnosticIds.IllegalLevelDependency,
			         ArchitecturalDiagnosticIds.ForbiddenDependency,
			         ArchitecturalDiagnosticIds.WrongDirectionDependency,
			         ArchitecturalDiagnosticIds.SameLayerDependency
		         })
		{
			yield return [site, diagnosticId];
		}
	}

	[Fact]
	public async Task AllowedSites_AllowsOnlyListedSite()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="Controller" to="Repository" allowedSites="Local" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository)
		                      {
		                          public void Run()
		                          {
		                              CandyRepository local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Constructor");
		diagnostic.GetMessage().Should().Contain("<AllowedDependency from=\"Controller\" to=\"Repository\"/> is configured, but allowedSites does not include Constructor");
	}

	[Fact]
	public async Task BlockedSites_BlocksOnlyListedSite()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="Controller" to="Repository" blockedSites="Local" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository)
		                      {
		                          public void Run()
		                          {
		                              CandyRepository local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Local");
		diagnostic.GetMessage().Should().Contain("<AllowedDependency from=\"Controller\" to=\"Repository\"/> is configured, but blockedSites blocks Local");
	}

	[Fact]
	public async Task AllowedSites_AreCommaSeparatedTrimmedAndCaseInsensitive()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="Controller" to="Repository" allowedSites=" local , METHODRETURN " />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController
		                      {
		                          private readonly CandyRepository _field = null!;
		                          public CandyRepository GetRepository() => null!;
		                          public void Run()
		                          {
		                              CandyRepository local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Field");
	}

	[Fact]
	public async Task UnknownAllowedSitesToken_IgnoresEdgeFailClosed()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="Controller" to="Repository" allowedSites="Constructor, SomewhereElse" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task BothSiteFiltersOnSameEdge_IgnoresEdgeFailClosed()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="Controller" to="Repository" allowedSites="Constructor" blockedSites="Field" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task WildcardTargetEdges_RespectSiteFilters()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="*" to="Repository" allowedSites="Local" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository)
		                      {
		                          public void Run()
		                          {
		                              CandyRepository local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Constructor");
		diagnostic.GetMessage().Should().Contain("<AllowedDependency from=\"*\" to=\"Repository\"/> is configured, but allowedSites does not include Constructor");
	}

	[Fact]
	public async Task WildcardSourceEdges_RespectSiteFilters()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                          <AllowedDependency from="Controller" to="*" blockedSites="Local" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController(CandyRepository repository)
		                      {
		                          public void Run()
		                          {
		                              CandyRepository local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Local");
		diagnostic.GetMessage().Should().Contain("<AllowedDependency from=\"Controller\" to=\"*\"/> is configured, but blockedSites blocks Local");
	}

	[Fact]
	public async Task ReverseEdgeDetection_IgnoresReverseEdgeSiteFilter()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Application"><Class typeName="CandyService" /></Layer>
		                          <AllowedDependency from="Application" to="Controller" allowedSites="Local" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyService { }
		                      public class CandyController(CandyService service) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Constructor");
	}

	[Fact]
	public async Task LocalSite_IsCheckedWithoutRootOptIn()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController
		                      {
		                          public void Run()
		                          {
		                              CandyRepository local = null!;
		                              _ = local;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency).Which;
		diagnostic.Properties["Site"].Should().Be("Local");
	}

	[Fact]
	public async Task LocalSite_CatchesExplicitAndInferredLocals()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Controller"><Class typeName="CandyController" /></Layer>
		                          <Layer name="Repository"><Class typeName="CandyRepository" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CandyRepository { }
		                      public class CandyController
		                      {
		                          public void Run()
		                          {
		                              CandyRepository explicitLocal = null!;
		                              var inferredLocal = explicitLocal;
		                              _ = inferredLocal;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency && d.Properties["Site"] == "Local")
			.Should().HaveCount(2);
	}

	[Theory]
	[MemberData(nameof(AllSitesAndLayeringDiagnostics))]
	public async Task EverySite_CanProduceLayeringDiagnostic(string site, string diagnosticId)
	{
		var dependencyTypeName = GetDependencyTypeName(diagnosticId);
		var source = CreateSourceForSite(site, dependencyTypeName);
		var config = CreateConfigForDiagnostic(diagnosticId);

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(d => d.Id == diagnosticId).Which;
		diagnostic.Properties["Site"].Should().Be(site);
	}

	private static IEnumerable<string> AllSites()
	{
		yield return "Constructor";
		yield return "Method";
		yield return "MethodReturn";
		yield return "Field";
		yield return "Property";
		yield return "Local";
		yield return "New";
		yield return "GenericInvocation";
		yield return "GenericArgument";
	}

	private static string GetDependencyTypeName(string diagnosticId)
    {
        var result = diagnosticId switch
        {
            ArchitecturalDiagnosticIds.IllegalLevelDependency => "DependencyRepository",
            ArchitecturalDiagnosticIds.ForbiddenDependency => "ForbiddenDependency",
            ArchitecturalDiagnosticIds.WrongDirectionDependency => "DependencyApplication",
            ArchitecturalDiagnosticIds.SameLayerDependency => "OtherController",
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticId), diagnosticId, null)
        };

		return result;
    }

    private static string CreateConfigForDiagnostic(string diagnosticId)
    {
        var result = diagnosticId switch
        {
            ArchitecturalDiagnosticIds.IllegalLevelDependency => """
                                                                 <ArchitecturalLevels>
                                                                     <Layer name="Controller"><Class typeName="CallerController" /></Layer>
                                                                     <Layer name="Repository"><Class typeName="DependencyRepository" /></Layer>
                                                                 </ArchitecturalLevels>
                                                                 """,
            ArchitecturalDiagnosticIds.ForbiddenDependency => """
                                                              <ArchitecturalLevels>
                                                                  <Layer name="Controller"><Class typeName="CallerController" /></Layer>
                                                                  <Forbidden><Class typeName="ForbiddenDependency" /></Forbidden>
                                                              </ArchitecturalLevels>
                                                              """,
            ArchitecturalDiagnosticIds.WrongDirectionDependency => """
                                                                   <ArchitecturalLevels>
                                                                       <Layer name="Controller"><Class typeName="CallerController" /></Layer>
                                                                       <Layer name="Application"><Class typeName="DependencyApplication" /></Layer>
                                                                       <AllowedDependency from="Application" to="Controller" />
                                                                   </ArchitecturalLevels>
                                                                   """,
            ArchitecturalDiagnosticIds.SameLayerDependency => """
                                                              <ArchitecturalLevels>
                                                                  <Layer name="Controller">
                                                                      <Class typeName="CallerController" />
                                                                      <Class typeName="OtherController" />
                                                                  </Layer>
                                                              </ArchitecturalLevels>
                                                              """,
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticId), diagnosticId, null)
        };

		return result;
    }

    private static string CreateSourceForSite(string site, string dependencyTypeName)
	{
		var caller = site switch
		{
			"Constructor" => $"public class CallerController({dependencyTypeName} dependency) {{ }}",
			"Method" => $"public class CallerController {{ public void Use({dependencyTypeName} dependency) {{ }} }}",
			"MethodReturn" => $"public class CallerController {{ public {dependencyTypeName} Get() => null!; }}",
			"Field" => $"public class CallerController {{ private readonly {dependencyTypeName} _dependency = null!; }}",
			"Property" => $"public class CallerController {{ public {dependencyTypeName} Dependency {{ get; set; }} = null!; }}",
			"Local" => $"public class CallerController {{ public void Run() {{ {dependencyTypeName} dependency = null!; _ = dependency; }} }}",
			"New" => $"public class CallerController {{ public void Run() => _ = new {dependencyTypeName}(); }}",
			"GenericInvocation" => $"public class CallerController {{ public void Run() => _ = Resolve<{dependencyTypeName}>(); private static T Resolve<T>() where T : class => null!; }}",
			"GenericArgument" => $"using System; public class CallerController(Lazy<{dependencyTypeName}> dependency) {{ }}",
			_ => throw new ArgumentOutOfRangeException(nameof(site), site, null)
		};

		return $$"""
		         {{caller}}
		         public class {{dependencyTypeName}} { }
		         """;
	}
}
