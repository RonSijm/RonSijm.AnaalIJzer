using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed class NameMatcherTests
{
	// ---- startsWith / contains matching ----

	[Fact]
	public async Task StartsWith_MatchesLayerCorrectly_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Handler">
		                              <Class startsWith="IHandle" />
		                          </Layer>
		                          <Layer name="Service">
		                              <Class startsWith="IService" />
		                          </Layer>
		                          <AllowedDependency from="Handler" to="Service" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IServicePatient { }
		                      public class IHandleRequest(IServicePatient svc) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task Contains_MatchesLayerCorrectly_ReportsARCH005()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class contains="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class contains="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Manager" to="Repository" />
		                      </ArchitecturalLevels>
		                      """;

		// OtherManager contains "Manager" -> same layer as PatientManager -> ARCH005
		const string source = """
		                      public class OtherManagerImpl { }
		                      public class PatientManagerImpl(OtherManagerImpl other) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.Should().NotBeEmpty();
	}

	// ---- Namespace matching ----

	[Fact]
	public async Task NamespaceEndsWith_MatchesLayerCorrectly_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Namespace endsWith=".Application" />
		                          </Layer>
		                          <Layer name="Infrastructure">
		                              <Namespace endsWith=".Infrastructure" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Infrastructure" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace MyApp.Infrastructure { public class DbWorker { } }
		                      namespace MyApp.Application    { public class UseCase(MyApp.Infrastructure.DbWorker db) { } }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task FileScopedNamespace_MatchesLayerCorrectly_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Layer name="Application">
		                              <Namespace exactName="MyApp.Application" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Repository" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace MyApp.Application;

		                      public class CheeseRepository { }
		                      public class PizzaKitchen(CheeseRepository cheese) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task NamespaceContains_WrongDirection_ReportsARCH004()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Namespace endsWith=".Application" />
		                          </Layer>
		                          <Layer name="Infrastructure">
		                              <Namespace endsWith=".Infrastructure" />
		                          </Layer>
		                          <AllowedDependency from="Application" to="Infrastructure" />
		                      </ArchitecturalLevels>
		                      """;

		// Infrastructure depending on Application is the wrong direction.
		const string source = """
		                      namespace MyApp.Application    { public class UseCase { } }
		                      namespace MyApp.Infrastructure { public class DbWorker(MyApp.Application.UseCase uc) { } }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency)
			.Should().NotBeEmpty();
	}

	// ---- Exact type name matching (typeName=) ----

	[Fact]
	public async Task ExactTypeName_ValidEdge_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="PartnerStore">
		                              <Class typeName="IPartnerStore" />
		                          </Layer>
		                          <AllowedDependency from="Manager" to="PartnerStore" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientManager(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ExactTypeName_Forbidden_ReportsARCH003WithComment()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class typeName="IIdentityContext"
		                                     comment="Controllers should unwrap IIdentityContext to a DTO." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IIdentityContext { }
		                      public class PatientManager(IIdentityContext identity) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch003 = diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).ToList();
		arch003.Count.Should().Be(1);
		arch003[0].GetMessage(CultureInfo.InvariantCulture)
			.Should().Contain("Controllers should unwrap IIdentityContext to a DTO.");
	}

	[Fact]
	public async Task ExactTypeName_TakesPrecedenceOverSuffix()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="IdentityContext">
		                              <Class endsWith="IdentityContext" />
		                          </Layer>
		                          <Forbidden>
		                              <Class typeName="IIdentityContext"
		                                     comment="Use the DTO, not the raw interface." />
		                          </Forbidden>
		                          <AllowedDependency from="Manager" to="IdentityContext" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IIdentityContext { }
		                      public class PatientManager(IIdentityContext identity) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().NotBeEmpty();
	}

	// ---- Regex matching ----

	[Fact]
	public async Task Regex_OnClass_MatchesAnchoredPattern()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class regex="^I[A-Z][A-Za-z0-9]*Handler$"
		                                     comment="Direct handler dependencies are not allowed." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPatientHandler { }
		                      public class PatientManager(IPatientHandler h) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Regex_OnClass_DoesNotMatchWhenAnchorsExcludeIt()
	{
		// Pattern only matches names that START with 'I' and end with 'Handler'.
		// 'PatientHandlerImpl' is excluded because it doesn't end with 'Handler'.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class regex="^I[A-Z][A-Za-z0-9]*Handler$" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PatientHandlerImpl { }
		                      public class PatientManager(PatientHandlerImpl h) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}

	[Fact]
	public async Task Regex_OnNamespace_MatchesInternalSegment()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Namespace regex="\.Internal(\.|$)"
		                                         comment="Don't reach into *.Internal namespaces." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace MyApp.Foo.Internal      { public class Hidden { } }
		                      namespace MyApp.Foo.InternalShared { public class Allowed { } }
		                      namespace MyApp
		                      {
		                          using MyApp.Foo.Internal;
		                          using MyApp.Foo.InternalShared;
		                          public class OrderManager(Hidden bad, Allowed ok) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("Hidden");
	}

	[Fact]
	public async Task ContainsAndRegex_AreConjunctive()
	{
		// contains matches, but the anchored regex requires text before Legacy and does not.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class contains="Legacy" regex="^.+Legacy.+$" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyStore { }
		                      public class OrderManager(LegacyStore s) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency);
	}

	[Fact]
	public async Task Regex_InvalidPattern_IsSilentlyIgnored()
	{
		// '[' is an invalid pattern; the rule should simply not match anything
		// instead of crashing the analyzer.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class regex="[" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class SomeTool { }
		                      public class OrderManager(SomeTool t) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}
}
