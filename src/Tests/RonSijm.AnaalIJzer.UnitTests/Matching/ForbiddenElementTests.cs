using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed class ForbiddenElementTests
{
	[Fact]
	public async Task Forbidden_ReportsARCH003WithCustomComment()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Layer name="Repository">
		                              <Class endsWith="Repository" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="IdentityContext"
		                                     comment="Controllers should unwrap IIdentityContext to a DTO." />
		                          </Forbidden>
		                          <AllowedDependency from="Manager" to="Repository" />
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IIdentityContext { }
		                      public class PatientRepository { }
		                      public class PatientManager(PatientRepository repo, IIdentityContext identity) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch003 = diagnostics.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).ToList();
		arch003.Count.Should().Be(1);

		var msg = arch003[0].GetMessage(CultureInfo.InvariantCulture);
		msg.Should().Contain("IIdentityContext");
		msg.Should().Contain("Controllers should unwrap IIdentityContext to a DTO.");
	}

	[Fact]
	public async Task Forbidden_WithoutComment_ReportsARCH003WithNoHint()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="IdentityContext" />
		                          </Forbidden>
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
}
