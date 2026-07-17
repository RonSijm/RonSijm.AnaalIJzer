using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Analysis;

public sealed class ValidDependencyTests
{
	[Fact]
	public async Task ValidDependency_ControllerToManager_NoDiagnostic()
	{
		const string source = """
		                      public class PatientManager { }
		                      public class PatientController(PatientManager manager) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ValidDependency_ManagerToRepository_NoDiagnostic()
	{
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientManager
		                      {
		                          public PatientManager(PatientRepository repo) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ValidDependency_UnleveledType_IsIgnored()
	{
		const string source = """
		                      public class SomeHelperService { }
		                      public class PatientController(SomeHelperService helper) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ValidDependency_ControllerWithMixedParams_OnlyFlagsLeveledOnes()
	{
		const string source = """
		                      using System.Threading;
		                      public class PatientManager { }
		                      public class SomeService { }
		                      public class PatientController
		                      {
		                          public PatientController(PatientManager manager, SomeService service, CancellationToken ct) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.DefaultConfig);

		diagnostics.Should().BeEmpty();
	}
}
