using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Operations;

public sealed class BehavioralOperationPolicyAnalyzerTests
{
    [Fact]
    public async Task RequiredOperation_DominanceReportsWhenTheRequiredCallOnlyOccursOnOneBranch()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit(bool isReady)
				{
					if (isReady)
					{
						PizzaValidator.Validate();
					}

					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperation>
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaValidator" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationRequiredMissing).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyBehavioralOperationViolationKind].Should().Be("RequiredOperationDoesNotDominateExit");
        violation.Properties[ArchitecturalDiagnostics.PropertyBehavioralOperationOrdering].Should().Be("Dominance");
        violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("Submit");
    }

    [Fact]
    public async Task RequiredOperationBefore_DominanceReportsTheUnvalidatedMutation()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit(bool isReady)
				{
					if (isReady)
					{
						PizzaValidator.Validate();
					}

					PizzaRepository.Save();
				}
			}

			public static class PizzaValidator { public static void Validate() { } }
			public static class PizzaRepository { public static void Save() { } }
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperationBefore>
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaValidator" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			        <BeforeOperation>
			          <OperationMatcher kind="Invocation">
			            <ContainingType typeName="PizzaRepository" />
			            <Member exactName="Save" memberKind="Method" />
			          </OperationMatcher>
			        </BeforeOperation>
			      </RequiredOperationBefore>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationOrdering).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyBehavioralOperationViolationKind].Should().Be("MissingRequiredOperationBefore");
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName].Should().Contain("Save");
    }

    [Fact]
    public async Task ForbiddenOperationAfter_ReportsTheOperationAfterTheTerminalCall()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit()
				{
					PizzaRepository.Commit();
					PizzaAudit.Record();
				}
			}

			public static class PizzaRepository { public static void Commit() { } }
			public static class PizzaAudit { public static void Record() { } }
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <ForbiddenOperationAfter>
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaAudit" />
			          <Member exactName="Record" memberKind="Method" />
			        </OperationMatcher>
			        <AfterOperation>
			          <OperationMatcher kind="Invocation">
			            <ContainingType typeName="PizzaRepository" />
			            <Member exactName="Commit" memberKind="Method" />
			          </OperationMatcher>
			        </AfterOperation>
			      </ForbiddenOperationAfter>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationOrdering).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyBehavioralOperationViolationKind].Should().Be("ForbiddenOperationAfter");
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName].Should().Contain("Record");
    }

    [Fact]
    public async Task MaximumOperationCount_ReportsEveryOccurrenceBeyondTheConfiguredLimit()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit()
				{
					PizzaPublisher.Publish();
					PizzaPublisher.Publish();
				}
			}

			public static class PizzaPublisher { public static void Publish() { } }
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <MaximumOperationCount maximum="1">
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaPublisher" />
			          <Member exactName="Publish" memberKind="Method" />
			        </OperationMatcher>
			      </MaximumOperationCount>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationCardinality).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyBehavioralOperationViolationKind].Should().Be("MaximumOperationCountExceeded");
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationDisplayName].Should().Contain("Publish");
    }

    [Fact]
    public async Task BehavioralOperations_ApplyFromAnOuterLayerToNestedLayers()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit() { }
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperation ordering="Lexical">
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			    </BehavioralOperations>
			    <Layer name="Kitchen">
			      <Class endsWith="Kitchen" />
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationRequiredMissing).Subject;
        violation.GetMessage().Should().Contain("layer Application/Kitchen");
        violation.Properties[ArchitecturalDiagnostics.PropertyViolationReason].Should().Contain("layer 'Application'");
    }

    [Fact]
    public async Task BehavioralOperations_DoNotApplyToADifferentDeclaration()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public void Submit() { }
				public void Inspect() { }
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperation ordering="Lexical">
			        <DeclarationMatcher>
			          <Member exactName="Submit" memberKind="Method" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.OperationRequiredMissing).ToArray();
        violations.Should().ContainSingle();
        violations[0].Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("Submit");
    }

    [Fact]
    public async Task BehavioralOperations_ApplyToAnExpressionBodiedPropertyAccessor()
    {
        const string source = """
			namespace Shop.Application;

			public sealed class PizzaKitchen
			{
				public string MenuPizza => PizzaMenu.Lookup();
			}

			public static class PizzaSafetyCheck { public static void Validate() { } }
			public static class PizzaMenu { public static string Lookup() => "Margherita"; }
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <BehavioralOperations>
			      <RequiredOperation ordering="Lexical">
			        <DeclarationMatcher>
			          <Member exactName="MenuPizza" memberKind="Property" />
			        </DeclarationMatcher>
			        <OperationMatcher kind="Invocation">
			          <ContainingType typeName="PizzaSafetyCheck" />
			          <Member exactName="Validate" memberKind="Method" />
			        </OperationMatcher>
			      </RequiredOperation>
			    </BehavioralOperations>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationRequiredMissing).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("get_MenuPizza");
        violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be("Declaration");
    }
}