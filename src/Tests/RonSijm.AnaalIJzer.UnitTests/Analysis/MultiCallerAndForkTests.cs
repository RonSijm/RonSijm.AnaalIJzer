using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Analysis;

public sealed class MultiCallerAndForkTests
{
	// ---- Multiple callers for same layer ----

	[Fact]
	public async Task MultipleCallers_DepValidForBothCallerLayers_NoDiagnostic()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Layer name="PartnerStore">
		                              <Class endsWith="PartnerStore" />
		                          </Layer>
		                          <AllowedDependency from="Manager"    to="Repository" />
		                          <AllowedDependency from="Manager"    to="PartnerStore" />
		                          <AllowedDependency from="Repository" to="PartnerStore" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientRepository { }
		                      public class PatientManager(PatientRepository repo, IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task MultipleCallers_DepNotCallableFromUnlistedCaller_ReportsARCH001()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Layer name="PartnerStore">
		                              <Class endsWith="PartnerStore" />
		                          </Layer>
		                          <AllowedDependency from="Manager"    to="Repository" />
		                          <AllowedDependency from="Repository" to="PartnerStore" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientManager(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}

	// ---- Fork architecture ----

	[Fact]
	public async Task Fork_ApplicationToDataAbstraction_Valid()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="DataAbstraction">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Layer name="Database">
		                              <Class endsWith="DbContext" />
		                          </Layer>
		                          <Layer name="ServiceAgent">
		                              <Class endsWith="ServiceAgent" />
		                          </Layer>
		                          <AllowedDependency from="Application"     to="DataAbstraction" />
		                          <AllowedDependency from="Application"     to="ServiceAgent" />
		                          <AllowedDependency from="DataAbstraction" to="Database" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PatientRepository { }
		                      public class PatientManager(PatientRepository repo) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task Fork_ServiceAgentToDatabase_ReportsARCH001()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="DataAbstraction">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Layer name="Database">
		                              <Class endsWith="DbContext" />
		                          </Layer>
		                          <Layer name="ServiceAgent">
		                              <Class endsWith="ServiceAgent" />
		                          </Layer>
		                          <AllowedDependency from="Application"     to="DataAbstraction" />
		                          <AllowedDependency from="Application"     to="ServiceAgent" />
		                          <AllowedDependency from="DataAbstraction" to="Database" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PatientDbContext { }
		                      public class SomeServiceAgent(PatientDbContext db) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.Should().NotBeEmpty();
	}
}
