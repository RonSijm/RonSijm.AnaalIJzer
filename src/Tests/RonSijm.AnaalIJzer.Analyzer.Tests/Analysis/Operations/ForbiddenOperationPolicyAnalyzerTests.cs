using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Operations;

public sealed class ForbiddenOperationPolicyAnalyzerTests
{
	[Fact]
	public async Task ForbiddenOperations_ResolveAliasedAndFullyQualifiedStaticProperties()
	{
		const string source = """
			using System;
			using Clock = System.DateTime;

			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare()
				{
					var first = Clock.UtcNow;
					var second = global::System.DateTime.UtcNow;
					return DateTime.Today;
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ForbiddenOperations description="Application code receives time through an adapter.">
			      <ForbiddenOperation allowedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="UtcNow" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed).ToArray();
		violations.Should().HaveCount(2);
		violations.Should().OnlyContain(item => item.Properties[ArchitecturalDiagnostics.PropertyOperationKind] == "PropertyRead");
		violations.Should().OnlyContain(item => item.Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName]!.Contains("UtcNow", StringComparison.Ordinal));
		violations.Should().OnlyContain(item => item.Properties[ArchitecturalDiagnostics.PropertySite] == "StaticMember");
	}

	[Fact]
	public async Task ForbiddenOperations_RestrictSelectedInstanceMembersAtTheirSemanticSites()
	{
		const string source = """
			using System.Threading.Tasks;

			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Prepare(Task<int> preparation)
				{
					preparation.Wait();
					var result = preparation.Result;
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ForbiddenOperations>
			      <ForbiddenOperation allowedSites="Method, Local">
			        <OperationMatcher kind="Invocation" staticAccess="false">
			          <ContainingType typeName="Task" />
			          <Member exactName="Wait" memberKind="Method" />
			        </OperationMatcher>
			        <OperationMatcher kind="PropertyRead" staticAccess="false">
			          <ContainingType typeName="Task" />
			          <Member exactName="Result" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed).ToArray();
		violations.Should().HaveCount(2);
		violations.Should().Contain(item => item.Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName]!.Contains("Wait", StringComparison.Ordinal)
			&& item.Properties[ArchitecturalDiagnostics.PropertySite] == "Method");
		violations.Should().Contain(item => item.Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName]!.Contains("Result", StringComparison.Ordinal)
			&& item.Properties[ArchitecturalDiagnostics.PropertySite] == "Local");
	}

	[Fact]
	public async Task ForbiddenOperations_SiteFiltersCanExcludeStaticMemberAccess()
	{
		const string source = """
			using System;

			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare() => DateTime.UtcNow;
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ForbiddenOperations>
			      <ForbiddenOperation blockedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="UtcNow" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed);
	}

	[Fact]
	public async Task ForbiddenOperations_ApplyFromAnOuterLayerToNestedLayers()
	{
		const string source = """
			using System;

			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public DateTime Prepare() => DateTime.Now;
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ForbiddenOperations>
			      <ForbiddenOperation allowedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.DateTime" />
			          <Member exactName="Now" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			    <Layer name="Kitchen">
			      <Class endsWith="Kitchen" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed).Subject;
		violation.GetMessage().Should().Contain("layer Application/Kitchen");
		violation.Properties[ArchitecturalDiagnostics.PropertyViolationReason].Should().Contain("layer 'Application'");
	}

	[Fact]
	public async Task ForbiddenOperations_CanForbidServiceLocationOutsideTheCompositionRoot()
	{
		const string source = """
			using System;

			namespace Shop;

			public sealed class PizzaKitchen
			{
				public object? Prepare(IServiceProvider services) => services.GetService(typeof(PizzaKitchen));
			}

			public sealed class PizzaCompositionRoot
			{
				public object? Compose(IServiceProvider services) => services.GetService(typeof(PizzaKitchen));
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			    <ForbiddenOperations>
			      <ForbiddenOperation allowedSites="MethodReturn">
			        <OperationMatcher kind="Invocation" staticAccess="false">
			          <ContainingType exactFullName="System.IServiceProvider" />
			          <Member exactName="GetService" memberKind="Method" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			  <Layer name="Composition">
			    <Class endsWith="CompositionRoot" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed).ToArray();
		violations.Should().ContainSingle();
		violations[0].Properties[ArchitecturalDiagnostics.PropertyCallerTypeName].Should().Be("PizzaKitchen");
	}

	[Fact]
	public async Task ForbiddenOperations_CanForbidOneEnvironmentMemberWhileLeavingOthersAvailable()
	{
		const string source = """
			using System;

			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public string Prepare()
				{
					var machine = Environment.MachineName;
					return Environment.NewLine;
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ForbiddenOperations>
			      <ForbiddenOperation allowedSites="StaticMember">
			        <OperationMatcher kind="PropertyRead" staticAccess="true">
			          <ContainingType exactFullName="System.Environment" />
			          <Member exactName="MachineName" memberKind="Property" />
			        </OperationMatcher>
			      </ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed).ToArray();
		violations.Should().ContainSingle();
		violations[0].Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName].Should().Contain("MachineName");
	}

	[Theory]
	[InlineData("""<OperationMatcher kind="PizzaTeleport" />""")]
	[InlineData("""<OperationMatcher kind="Invocation"><Member memberKind="Sauce" /></OperationMatcher>""")]
	[InlineData("""<OperationMatcher kind="Invocation" staticAccess="sometimes" />""")]
	[InlineData("""<OperationMatcher kind="PropertyRead"><Member exactName="UtcNow" memberKind="Method" /></OperationMatcher>""")]
	public async Task InvalidForbiddenOperations_ReportConfigurationIssues(string matcher)
	{
		var config = $"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Kitchen" />
			    <ForbiddenOperations>
			      <ForbiddenOperation>{matcher}</ForbiddenOperation>
			    </ForbiddenOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public sealed class PizzaKitchen { public void Prepare() { } }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.ConfigurationInvalid);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.OperationNotAllowed);
	}
}
