
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

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

	[Theory]
	[InlineData("public", "PublicTool")]
	[InlineData("internal", "InternalTool")]
	[InlineData("private", "PrivateTool")]
	[InlineData("protected", "ProtectedTool")]
	[InlineData("sealed", "SealedTool")]
	[InlineData("abstract", "AbstractTool")]
	[InlineData("static", "StaticTool")]
	[InlineData("record", "RecordTool")]
	public void WithAccessModifier_MatchesEverySupportedModifierToken(string modifier, string typeName)
	{
		const string source = """
		                      namespace ModifierSamples
		                      {
		                          public class PublicTool { }
		                          internal class InternalTool { }
		                          public sealed class SealedTool { }
		                          public abstract class AbstractTool { }
		                          public static class StaticTool { }
		                          public record RecordTool;

		                          public class Host
		                          {
		                              private class PrivateTool { }
		                              protected class ProtectedTool { }
		                          }
		                      }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, typeName);
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAccessModifier, modifier);

		matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol)
			.Should().Be(string.Empty);
	}

	[Fact]
	public void WithAccessModifier_UnsupportedTokenDoesNotMatch()
	{
		const string source = """
		                      namespace ModifierSamples
		                      {
		                          public class PublicTool { }
		                      }
		                      """;

		var symbol = GetDeclaredTypeSymbol(source, "PublicTool");
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.HasAccessModifier, "friend");

		matcher.TryMatch(symbol.Name, symbol.ContainingNamespace.ToDisplayString(), symbol)
			.Should().BeNull();
	}

	// ---- Exceptions interop ----

}
