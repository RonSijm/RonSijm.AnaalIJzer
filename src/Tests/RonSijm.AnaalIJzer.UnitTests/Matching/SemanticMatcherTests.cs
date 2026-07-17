using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed partial class SemanticMatcherTests
{
	// ---- exactName / exactFullName ----

	[Fact]
	public async Task ExactName_OnClass_IsSynonymOfTypeName()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class exactName="IIdentityContext"
		                                     comment="Use the DTO." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IIdentityContext { }
		                      public class PatientManager(IIdentityContext id) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task ExactFullName_MatchesNamespaceQualifiedType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class exactFullName="App.Legacy.LegacyHelper"
		                                     comment="Migrate away." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Legacy { public class LegacyHelper { } }
		                      namespace App
		                      {
		                          using App.Legacy;
		                          public class OrderManager(LegacyHelper helper) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task ExactFullName_DoesNotMatchSimpleName()
	{
		// 'LegacyHelper' is in App.Core, but the rule requires App.Legacy.LegacyHelper.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class exactFullName="App.Legacy.LegacyHelper" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Core { public class LegacyHelper { } }
		                      namespace App
		                      {
		                          using App.Core;
		                          public class OrderManager(LegacyHelper helper) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}

	[Fact]
	public async Task ExactName_OnNamespace_MatchesExactNamespaceString()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Namespace exactName="App.Legacy" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      namespace App.Legacy { public class Helper { } }
		                      namespace App.LegacyShared { public class OtherHelper { } }
		                      namespace App
		                      {
		                          using App.Legacy;
		                          using App.LegacyShared;
		                          public class OrderManager(Helper bad, OtherHelper allowed) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("Helper");
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().NotContain("OtherHelper");
	}

	// ---- inherits ----

	[Fact]
	public async Task Inherits_MatchesDirectBaseType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class inherits="LegacyBase" comment="Drop the LegacyBase hierarchy." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyBase { }
		                      public class OldThing : LegacyBase { }
		                      public class OrderManager(OldThing thing) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Inherits_MatchesTransitiveBaseType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class inherits="LegacyBase" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyBase { }
		                      public class Intermediate : LegacyBase { }
		                      public class GrandChild : Intermediate { }
		                      public class OrderManager(GrandChild child) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Inherits_DoesNotMatchUnrelatedType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class inherits="LegacyBase" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class LegacyBase { }
		                      public class CleanThing { }
		                      public class OrderManager(CleanThing thing) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	// ---- implements ----

	[Fact]
	public async Task Implements_MatchesDirectInterface()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IDomainEvent"
		                                     comment="Managers must not depend on raw domain events." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IDomainEvent { }
		                      public class PatientAdmitted : IDomainEvent { }
		                      public class OrderManager(PatientAdmitted ev) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Implements_MatchesTransitiveInterface()
	{
		// IFoo : IBase, ConcreteFoo : IFoo -> ConcreteFoo transitively implements IBase.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IBase" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IBase { }
		                      public interface IFoo : IBase { }
		                      public class ConcreteFoo : IFoo { }
		                      public class OrderManager(ConcreteFoo foo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task Implements_DoesNotMatchUnrelatedInterface()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IDomainEvent" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IDomainEvent { }
		                      public interface IQueryModel { }
		                      public class ReadModel : IQueryModel { }
		                      public class OrderManager(ReadModel model) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().BeEmpty();
	}

	// ---- withAttribute ----

}
