namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class DependencySiteTests
{
	// ---- Site property ----

	[Fact]
	public async Task SiteProperty_Constructor_IsTagged()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientController(PatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		var d = diagnostics.First(x => x.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		d.Properties["Site"].Should().Be("Constructor");
	}

	[Fact]
	public async Task SiteProperty_Field_IsTagged()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController
		                      {
		                          private readonly IPatientRepository _repository = null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		var d = diagnostics.First(x => x.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		d.Properties["Site"].Should().Be("Field");
	}

	[Fact]
	public async Task SiteProperty_GenericArgument_OverridesOuterSite()
	{
		// The outer Lazy<> is not in any layer; the inner IPatientRepository is — and
		// triggers via the GenericArgument path, not the Constructor path.
		const string source = """
		                      using System;
		                      public interface IPatientRepository { }
		                      public class PatientController(Lazy<IPatientRepository> repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		var d = diagnostics.First(x => x.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		d.Properties["Site"].Should().Be("GenericArgument");
	}

	[Fact]
	public async Task SiteProperty_MethodReturn_IsTagged()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientController
		                      {
		                          public PatientRepository Get() => null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		var d = diagnostics.First(x => x.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
		d.Properties["Site"].Should().Be("MethodReturn");
	}

	// ---- Interface parameters ----

	[Fact]
	public async Task InterfaceParameter_MatchingBySuffix_IsDetected()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController(IPatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task InterfaceParameter_ValidEdge_NoDiagnostic()
	{
		const string source = """
		                      public interface IPatientManager { }
		                      public class PatientController(IPatientManager manager) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	// ---- Primary constructors ----

	[Fact]
	public async Task PrimaryConstructor_IllegalDependency_ReportsARCH005()
	{
		// OtherController and PatientController are both in the Controller layer.
		const string source = """
		                      public class OtherController { }
		                      public class PatientController(OtherController other);
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PrimaryConstructor_ValidDependency_NoDiagnostic()
	{
		const string source = """
		                      public class PatientManager { }
		                      public class PatientController(PatientManager manager);
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	// ---- Non-constructor dependency sites ----

	[Fact]
	public async Task FieldInjection_IllegalDependency_ReportsARCH001()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController
		                      {
		                          private readonly IPatientRepository _repository = null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task FieldInjection_ValidDependency_NoDiagnostic()
	{
		const string source = """
		                      public interface IPatientManager { }
		                      public class PatientController
		                      {
		                          private readonly IPatientManager _manager = null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task PropertyInjection_IllegalDependency_ReportsARCH001()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController
		                      {
		                          public IPatientRepository Repository { get; set; } = null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task MethodInjection_IllegalDependency_ReportsARCH001()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController
		                      {
		                          public void Handle(IPatientRepository repository) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task MethodReturn_IllegalDependency_ReportsARCH001()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController
		                      {
		                          public IPatientRepository GetRepository() => null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().ContainSingle()
			.Which.Properties["Site"].Should().Be("MethodReturn");
	}

	[Fact]
	public async Task MethodReturn_ValidDependency_NoDiagnostic()
	{
		const string source = """
		                      public interface IPatientManager { }
		                      public class PatientController
		                      {
		                          public IPatientManager GetManager() => null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ObjectCreation_NewingForbiddenLayer_ReportsARCH001()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientController
		                      {
		                          public void Run()
		                          {
		                              var repo = new PatientRepository();
		                              _ = repo;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ObjectCreation_ImplicitNew_ReportsARCH001()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientController
		                      {
		                          public PatientRepository Get() => new();
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ServiceLocator_GenericInvocation_ReportsARCH001()
	{
		const string source = """
		                      using System;
		                      public interface IPatientRepository { }
		                      public static class Locator
		                      {
		                          public static T Resolve<T>() where T : class => null!;
		                      }
		                      public class PatientController
		                      {
		                          public void Run()
		                          {
		                              var repo = Locator.Resolve<IPatientRepository>();
		                              _ = repo;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ServiceLocator_UnqualifiedGenericInvocation_ReportsARCH001()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class PatientController
		                      {
		                          private static T Resolve<T>() where T : class => null!;

		                          public void Run()
		                          {
		                              var repo = Resolve<IPatientRepository>();
		                              _ = repo;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Select(d => d.Properties["Site"])
			.Should().BeEquivalentTo(["GenericInvocation", "Local"]);
	}

	[Fact]
	public async Task ServiceLocator_NonGenericInvocation_IsIgnored()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public static class Locator
		                      {
		                          public static object Resolve() => null!;
		                      }
		                      public class PatientController
		                      {
		                          public void Run()
		                          {
		                              var repo = Locator.Resolve();
		                              _ = repo;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ServiceLocator_GenericInvocation_FromUnlayeredClass_IsIgnored()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public static class Locator
		                      {
		                          public static T Resolve<T>() where T : class => null!;
		                      }
		                      public class SomeHelper
		                      {
		                          public void Run()
		                          {
		                              var repo = Locator.Resolve<IPatientRepository>();
		                              _ = repo;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task FieldAndProperty_FromUnlayeredClass_AreIgnored()
	{
		const string source = """
		                      public interface IPatientRepository { }
		                      public class SomeHelper
		                      {
		                          private readonly IPatientRepository _repository = null!;
		                          public IPatientRepository Repository { get; set; } = null!;
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ForbiddenType_IsNotTreatedAsCaller()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CheeseRepository { }
		                      public class CheeseStore(CheeseRepository repository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task NonConstructorSites_StrictMode_DoNotEmitARCH002_ForUnleveledTypes()
	{
		// Strict ARCH002 should remain a ctor/method-parameter-only fallback so that
		// fields/properties/method returns/new/invocations don't drown projects in noise for primitives.
		const string source = """
		                      public class PatientManager
		                      {
		                          private readonly int _counter;
		                          public string Name { get; set; } = "";
		                          public string GetName() => Name;
		                          public void Run()
		                          {
		                              var x = new System.Text.StringBuilder();
		                              _ = x;
		                          }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.StrictConfig);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.Should().BeEmpty();
	}

	[Fact]
	public async Task NewingFromUnleveledClass_NoDiagnostic()
	{
		// The 'new' site is only checked when the enclosing class belongs to a layer.
		const string source = """
		                      public class PatientRepository { }
		                      public class SomeHelper
		                      {
		                          public void Run() => _ = new PatientRepository();
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}
}
