using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Matching;

public sealed partial class SemanticMatcherTests
{
	[Fact]
	public async Task WithAccessModifier_PublicMatchesPublicType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAccessModifier="public"
		                                     comment="Internal-only zone." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class PublicTool { }
		                      public class OrderManager(PublicTool tool) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAccessModifier_MultipleTokensRequireAll()
	{
		// "public sealed" should match only types that are both public AND sealed.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAccessModifier="public sealed" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public sealed class SealedTool { }
		                      public class NormalTool { }
		                      public class OrderManager(SealedTool s, NormalTool n) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("SealedTool");
	}

	// ---- Exceptions interop ----

}
