using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Matching;

public sealed partial class SemanticMatcherTests
{
	[Fact]
	public async Task Exceptions_SemanticMatcher_BypassesRule()
	{
		// 'implements="IDomainEvent"' forbids everything that implements the marker,
		// but the [LegacyEvent] attribute exempts grandfathered ones.
		const string config = """
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class implements="IDomainEvent">
		                                  <Exceptions>
		                                      <Class withAttribute="LegacyEvent" />
		                                  </Exceptions>
		                              </Class>
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      using System;
		                      public interface IDomainEvent { }
		                      [AttributeUsage(AttributeTargets.Class)]
		                      public sealed class LegacyEventAttribute : Attribute { }
		                      [LegacyEvent] public class OldEvent : IDomainEvent { }
		                      public class NewEvent : IDomainEvent { }
		                      public class OrderManager(OldEvent ok, NewEvent bad) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var forbidden = diagnostics
			.Where(d => d.Id == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.ToList();

		forbidden.Should().ContainSingle();
		forbidden[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("NewEvent");
	}
}
