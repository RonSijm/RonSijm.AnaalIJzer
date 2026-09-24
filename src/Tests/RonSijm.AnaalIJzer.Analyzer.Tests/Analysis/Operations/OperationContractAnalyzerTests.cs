using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.Operations;

public sealed class OperationContractAnalyzerTests
{
    [Fact]
    public async Task OwnerWithTheWrongResponse_ReportsShapeMismatch()
    {
        const string source = """
			public sealed class PizzaKitchen
			{
				public string PlacePizzaOrder(PlacePizzaOrderRequest request) => "queued";
			}

			public sealed class PlacePizzaOrderRequest { }
			public sealed class PlacePizzaOrderResponse { }
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(includeEntryPoint: false));

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationContractShapeMismatch).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationContractParticipantRole].Should().Be("Owner");
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationContractViolationKind].Should().Be("OwnerInvalidResponse");
    }

    [Fact]
    public async Task EntryPointThatDoesNotCallTheSelectedOwner_ReportsRequiredMissing()
    {
        const string source = """
			public sealed class PizzaOrderController
			{
				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request) => new();
			}

			public sealed class PizzaKitchen
			{
				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request) => new();
			}

			public sealed class PlacePizzaOrderRequest { }
			public sealed class PlacePizzaOrderResponse { }
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(includeEntryPoint: true));

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.OperationContractRequiredMissing).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationContractParticipantRole].Should().Be("EntryPoint");
        violation.Properties[ArchitecturalDiagnostics.PropertyOperationContractViolationKind].Should().Be("EntryPointDoesNotInvokeOwner");
    }

    [Fact]
    public async Task EntryPointThatCallsTheSelectedOwner_ProducesNoOperationContractDiagnostic()
    {
        const string source = """
			public sealed class PizzaOrderController
			{
				private readonly PizzaKitchen kitchen = new();

				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
				{
					return kitchen.PlacePizzaOrder(request);
				}
			}

			public sealed class PizzaKitchen
			{
				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request) => new();
			}

			public sealed class PlacePizzaOrderRequest { }
			public sealed class PlacePizzaOrderResponse { }
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig(includeEntryPoint: true));

        diagnostics.Should().NotContain(item => ArchitecturalDiagnosticIds.IsOperationContract(item.Id));
    }

    [Fact]
    public async Task EntryPointMatchedByTwoSelectors_IsReportedOnlyOnce()
    {
        const string source = """
			public sealed class PizzaOrderController
			{
				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request) => new();
			}

			public sealed class PizzaKitchen
			{
				public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request) => new();
			}

			public sealed class PlacePizzaOrderRequest { }
			public sealed class PlacePizzaOrderResponse { }
			""";
        var config = CreateConfig(includeEntryPoint: true).Replace(
            "</Operation>",
            """
			  <EntryPoint>
			    <DeclarationMatcher>
			      <ContainingType endsWith="Controller" />
			      <Member exactName="PlacePizzaOrder" memberKind="Method" />
			    </DeclarationMatcher>
			  </EntryPoint>
			</Operation>
			""",
            StringComparison.Ordinal);

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        diagnostics.Count(item => item.Id == ArchitecturalDiagnosticIds.OperationContractRequiredMissing).Should().Be(1);
    }

    private static string CreateConfig(bool includeEntryPoint)
    {
        var entryPoint = includeEntryPoint
            ? """
			  <EntryPoint>
			    <DeclarationMatcher>
			      <ContainingType endsWith="Controller" />
			      <Member exactName="PlacePizzaOrder" memberKind="Method" />
			    </DeclarationMatcher>
			  </EntryPoint>
			  """
            : string.Empty;
        var result = """
			<ArchitecturalLevels>
			  <Layer name="Controller"><Class endsWith="Controller" /></Layer>
			  <Layer name="Application"><Class endsWith="Kitchen" /></Layer>
			  <AllowedDependency from="Controller" to="Application" />
			  <Operations>
			    <Operation name="PlacePizzaOrder" allowedOwnerLayers="Application" allowedEntryPointLayers="Controller">
			      <Owner>
			        <DeclarationMatcher>
			          <ContainingType endsWith="Kitchen" />
			          <Member exactName="PlacePizzaOrder" memberKind="Method" />
			        </DeclarationMatcher>
			      </Owner>
			      <Request><Class exactName="PlacePizzaOrderRequest" /></Request>
			      <Response><Class exactName="PlacePizzaOrderResponse" /></Response>
			ENTRY_POINT
			    </Operation>
			  </Operations>
			</ArchitecturalLevels>
			""".Replace("ENTRY_POINT", entryPoint, StringComparison.Ordinal);

        return result;
    }
}