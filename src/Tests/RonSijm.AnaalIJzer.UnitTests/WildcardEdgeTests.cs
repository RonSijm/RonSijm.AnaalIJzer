namespace RonSijm.AnaalIJzer.UnitTests;

public sealed class WildcardEdgeTests
{
	[Fact]
	public async Task WildcardEdge_AllowedFromAnyLeveledCaller_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels strict="true">
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Layer name="Logger">
		                              <Class endsWith="Logger" />
		                          </Layer>
		                          <AllowedDependency from="Manager" to="Repository" />
		                          <AllowedDependency from="*"       to="Logger" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface ILogger { }
		                      public class PatientRepository { }
		                      public class PatientManager(PatientRepository repo, ILogger logger) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task WildcardEdge_AllowedToAnyLayer_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Diagnostics">
		                              <Class endsWith="Diagnostics" />
		                          </Layer>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Manager"     to="Repository" />
		                          <AllowedDependency from="Diagnostics" to="*" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PatientRepository { }
		                      public class PatientManager { }
		                      public class HealthDiagnostics(PatientManager mgr, PatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task WildcardEdge_AllowedToAnyLayer_DoesNotApplyToOtherCallers()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Diagnostics">
		                              <Class endsWith="Diagnostics" />
		                          </Layer>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="Diagnostics" to="*" />
		                      </ArchitecturalLevels>
		                      """;

		// Manager has no edge to Repository — the to="*" rule is for Diagnostics only.
		const string source = """
		                      public class PatientRepository { }
		                      public class PatientManager(PatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task WildcardEdge_AllowedToAnyLayer_ForbiddenStillReportsARCH003()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Diagnostics">
		                              <Class endsWith="Diagnostics" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" />
		                          </Forbidden>
		                          <AllowedDependency from="Diagnostics" to="*" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class HealthDiagnostics(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().NotBeEmpty();
	}

	[Fact]
	public async Task WildcardEdge_AllowAny_PermitsEveryInterLayerDependency()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <AllowedDependency from="*" to="*" />
		                      </ArchitecturalLevels>
		                      """;

		// Without the wildcard this would have been ARCH004 (Repository -> Manager goes up).
		const string source = """
		                      public class PatientManager { }
		                      public class PatientRepository(PatientManager mgr) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}
}
