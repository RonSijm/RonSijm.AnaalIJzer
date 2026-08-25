using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis;

[Collection(ArchitectureClockTestCollection.Name)]
public sealed class ExceptionPolicyAnalyzerTests
{
	[Fact]
	public async Task ExceptionPolicy_WhenMissingOwner_ReportsArch017()
	{
		using var _ = ArchitectureClock.Freeze(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc));

		const string config = """
		                      <ArchitecturalLevels>
		                        <ExceptionPolicy requireOwner="true" />
		                        <Layer name="Application">
		                          <Class endsWith="Manager">
		                            <Exceptions>
		                              <Class typeName="LegacyManager" />
		                            </Exceptions>
		                          </Class>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = "public class LegacyManager { }";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ExceptionReview).Subject;
		diagnostic.GetMessage().Should().Contain("missing required owner metadata");
		diagnostic.Properties[ArchitecturalDiagnostics.PropertyExceptionStatus].Should().Be(nameof(ArchitectureExceptionStatus.Invalid));
	}

	[Fact]
	public async Task ExceptionPolicy_WhenExpired_ExceptionFailsClosedAndUnderlyingViolationReturns()
	{
		using var _ = ArchitectureClock.Freeze(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc));

		const string config = """
		                      <ArchitecturalLevels>
		                        <ExceptionPolicy requireExpiresOn="true" />
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Repository">
		                          <Class endsWith="Repository">
		                            <Exceptions>
		                              <Class typeName="ICheeseRepository"
		                                     expiresOn="2026-06-30" />
		                            </Exceptions>
		                          </Class>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface ICheeseRepository { }
		                      public class PizzaController(ICheeseRepository cheeseRepository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.ExceptionReview && item.GetMessage().Contains("has expired on 2026-06-30", StringComparison.Ordinal));
		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task ExceptionPolicy_WhenExpiringSoon_WarnsButExceptionStaysActive()
	{
		using var _ = ArchitectureClock.Freeze(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc));

		const string config = """
		                      <ArchitecturalLevels>
		                        <ExceptionPolicy requireExpiresOn="true"
		                                         warnBeforeDays="14" />
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Repository">
		                          <Class endsWith="Repository">
		                            <Exceptions>
		                              <Class typeName="ICheeseRepository"
		                                     expiresOn="2026-08-01" />
		                            </Exceptions>
		                          </Class>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface ICheeseRepository { }
		                      public class PizzaController(ICheeseRepository cheeseRepository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.ExceptionReview && item.GetMessage().Contains("expires in 6 days on 2026-08-01", StringComparison.Ordinal));
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}

	[Fact]
	public async Task ExceptionPolicy_WhenOmitted_PreservesExistingExceptionBehavior()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Controller">
		                          <Class endsWith="Controller" />
		                        </Layer>
		                        <Layer name="Repository">
		                          <Class endsWith="Repository">
		                            <Exceptions>
		                              <Class typeName="ICheeseRepository" />
		                            </Exceptions>
		                          </Class>
		                        </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface ICheeseRepository { }
		                      public class PizzaController(ICheeseRepository cheeseRepository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ExceptionReview);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.IllegalLevelDependency);
	}
}
