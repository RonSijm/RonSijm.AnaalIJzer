using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed partial class SemanticMatcherTests
{
	[Fact]
	public async Task WithAttribute_MatchesWithoutAttributeSuffix()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="Obsolete"
		                                     comment="Remove the obsolete dependency." />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      [Obsolete] public class OldRegistry { }
		                      public class OrderManager(OldRegistry reg) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAttribute_MatchesWithAttributeSuffix()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="ObsoleteAttribute" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      [Obsolete] public class OldRegistry { }
		                      public class OrderManager(OldRegistry reg) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAttribute_MatchesFullyQualifiedAttributeName()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="App.Markers.LegacyIngredientAttribute" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      namespace App.Markers
		                      {
		                          [AttributeUsage(AttributeTargets.Class)]
		                          public sealed class LegacyIngredientAttribute : Attribute { }
		                      }

		                      namespace App
		                      {
		                          using App.Markers;
		                          [LegacyIngredient] public class OldTopping { }
		                          public class OrderManager(OldTopping topping) { }
		                      }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.Should().ContainSingle();
	}

	[Fact]
	public async Task WithAttribute_DoesNotMatchUndecoratedType()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class withAttribute="Obsolete" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public class CleanRegistry { }
		                      public class OrderManager(CleanRegistry reg) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	// ---- withAccessModifier ----

}
