using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

public sealed class OperationContractCodeFixTests
{
	[Fact]
	public void OperationContractViolation_IsNotListedAsFixable()
	{
		new ArchitecturalLevelCodeFixProvider()
			.FixableDiagnosticIds
			.Should()
			.NotContain(ArchitecturalDiagnosticIds.OperationContractViolation);
	}

	[Fact]
	public async Task OperationContractViolation_OffersNoSpeculativeCodeFix()
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
		const string config = """
			<ArchitecturalLevels>
			  <Operations>
			    <Operation name="PlacePizzaOrder">
			      <Owner>
			        <DeclarationMatcher>
			          <ContainingType exactName="PizzaKitchen" />
			          <Member exactName="PlacePizzaOrder" memberKind="Method" />
			        </DeclarationMatcher>
			      </Owner>
			      <EntryPoint>
			        <DeclarationMatcher>
			          <ContainingType endsWith="Controller" />
			          <Member exactName="PlacePizzaOrder" memberKind="Method" />
			        </DeclarationMatcher>
			      </EntryPoint>
			    </Operation>
			  </Operations>
			</ArchitecturalLevels>
			""";

		var titles = await AnalyzerTestHelper.GetCodeFixTitlesAsync(source, config, ArchitecturalDiagnosticIds.OperationContractViolation);

		titles.Should().BeEmpty();
	}
}
