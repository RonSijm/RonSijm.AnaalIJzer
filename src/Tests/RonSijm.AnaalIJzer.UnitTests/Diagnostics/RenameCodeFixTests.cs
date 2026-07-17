using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Diagnostics;

public sealed class RenameCodeFixTests
{
	[Fact]
	public void RenameCodeFix_HasNoFixAllProvider()
	{
		new ArchitecturalLevelCodeFixProvider()
			.GetFixAllProvider()
			.Should().BeNull();
	}

	[Fact]
	public async Task ForbiddenWithFix_DiagnosticContainsRenameProperties()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" comment="Use a Repository instead.">
		                                  <Fix Rename="Repository" />
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientManager(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch003 = diagnostics.First(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency);
		arch003.Properties[ArchitecturalLevelAnalyzer.PropertyMatchedSuffix].Should().Be("Store");
		arch003.Properties[ArchitecturalLevelAnalyzer.PropertyFixSuffix].Should().Be("Repository");
	}

	[Fact]
	public async Task ForbiddenWithoutFix_DiagnosticHasNoRenameProperties()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" comment="Use a Repository instead." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientManager(IPartnerStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch003 = diagnostics.First(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency);
		arch003.Properties.ContainsKey(ArchitecturalLevelAnalyzer.PropertyMatchedSuffix).Should().BeFalse();
		arch003.Properties.ContainsKey(ArchitecturalLevelAnalyzer.PropertyFixSuffix).Should().BeFalse();
	}

	[Fact]
	public async Task ForbiddenWithFix_ExactTypeNameMatch_NoRenameProperties()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class typeName="IIdentityContext">
		                                  <Fix Rename="IdentityDto" />
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IIdentityContext { }
		                      public class PatientManager(IIdentityContext identity) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var arch003 = diagnostics.First(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency);
		// Exact-name matches produce no MatchedSuffix, so the fixer will not offer a rename.
		arch003.Properties.ContainsKey(ArchitecturalLevelAnalyzer.PropertyMatchedSuffix).Should().BeFalse();
	}

	[Fact]
	public async Task ForbiddenWithFix_AppliesRenameCodeFix()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Manager">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" comment="Use a Repository instead.">
		                                  <Fix Rename="Repository" />
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPartnerStore { }
		                      public class PatientManager(IPartnerStore store) { }
		                      """;

		var newSource = await AnalyzerTestHelper.ApplyCodeFixAsync(source, config);

		newSource.Should().Contain("IPartnerRepository");
		newSource.Should().NotContain("IPartnerStore");
	}
}
