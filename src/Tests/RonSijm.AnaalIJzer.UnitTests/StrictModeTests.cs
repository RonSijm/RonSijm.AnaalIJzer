namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class StrictModeTests
{
	[Fact]
	public async Task StrictMode_UnrecognizedDependency_ReportsARCH002()
	{
		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientConsentRepository { }
		                      public class ActivatePatientConsentManager(PatientConsentRepository repo, IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.StrictConfig);

		var arch002 = diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency).ToList();
		arch002.Count.Should().Be(1);
		arch002[0].GetMessage(CultureInfo.InvariantCulture)
			.Should().Contain("IPartnerStore");
	}

	[Fact]
	public async Task StrictMode_RecognizedDependency_NoDiagnostic()
	{
		const string source = """
		                      public class PatientConsentRepository { }
		                      public class ActivatePatientConsentManager(PatientConsentRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.StrictConfig);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task NonStrictMode_UnrecognizedDependency_NoDiagnostic()
	{
		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientConsentRepository { }
		                      public class ActivatePatientConsentManager(PatientConsentRepository repo, IPartnerStore store) { }
		                      """;

		const string nonStrictConfig = """
		                               <ArchitecturalLevels>
		                                   <Layer name="Manager">
		                                       <Class endsWith="Manager" />
		                                   </Layer>
		                                   <Layer name="Repository">
		                                       <Class endsWith="Repository" />
		                                   </Layer>
		                                   <AllowedDependency from="Manager" to="Repository" />
		                               </ArchitecturalLevels>
		                               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, nonStrictConfig);

		diagnostics.Should().NotContain(d => d.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task StrictMode_UnleveledCallerWithUnrecognizedDep_NoDiagnostic()
	{
		const string source = """
		                      public interface IPartnerStore { }
		                      public class SomeHelper(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, TestConfigs.StrictConfig);

		diagnostics.Should().BeEmpty();
	}
}
