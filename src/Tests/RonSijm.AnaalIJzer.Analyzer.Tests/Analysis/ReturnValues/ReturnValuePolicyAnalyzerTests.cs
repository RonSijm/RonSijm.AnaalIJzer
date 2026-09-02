using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.ReturnValues;

public sealed class ReturnValuePolicyAnalyzerTests
{
	[Fact]
	public async Task ReturnValuePolicy_RejectsConfiguredLiteralReturnValues()
	{
		const string source = """
			namespace Shop.Application;

			public enum PizzaStatus
			{
				None,
				Ready
			}

			public sealed class PizzaService
			{
				public object ReturnNothing() => null!;

				public string ReturnEmpty() => "";

				public int ReturnSentinel() => 42;

				public PizzaStatus ReturnNone() => (PizzaStatus)0;

				public object ReturnPizza() => new object();
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ReturnValuePolicy>
			      <Literal value="null" />
			      <Literal value="" />
			      <Literal value="42" />
			      <Literal value="0" />
			    </ReturnValuePolicy>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.ReturnValuePolicyViolation).ToArray();
		violations.Should().HaveCount(4);
		violations.Should().OnlyContain(item => item.Properties[ArchitecturalDiagnostics.PropertyReturnValueRuleTarget] == "Literal");
		violations.Should().OnlyContain(item => item.Properties[ArchitecturalDiagnostics.PropertySite] == "MethodReturn");
	}

	[Fact]
	public async Task ReturnValuePolicy_RequiresConfiguredAnnotatedInvocationToBeHandled()
	{
		const string source = """
			namespace JetBrains.Annotations
			{
				[System.AttributeUsage(System.AttributeTargets.Method)]
				public sealed class CanBeNullAttribute : System.Attribute { }
			}

			namespace Shop.Application
			{
				public sealed class Pizza
				{
					public static Pizza Margherita { get; } = new Pizza();
				}

				public sealed class PizzaLookup
				{
					[JetBrains.Annotations.CanBeNull]
					public Pizza FindPizza() => Pizza.Margherita;
				}

				public sealed class PizzaService(PizzaLookup lookup)
				{
					public Pizza ReturnLookupDirectly() => lookup.FindPizza();

					public Pizza ReturnLookupWithFallback() => lookup.FindPizza() ?? Pizza.Margherita;
				}
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Namespace startsWith="Shop.Application" />
			    <ReturnValuePolicy description="Nullable lookup results are handled before leaving the kitchen.">
			      <Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" />
			    </ReturnValuePolicy>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ReturnValuePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyDeclaredSymbolName].Should().Be("ReturnLookupDirectly");
		violation.Properties[ArchitecturalDiagnostics.PropertyReturnValueRuleTarget].Should().Be("Invocation");
		violation.Properties[ArchitecturalDiagnostics.PropertyReturnValueRule].Should().Contain("JetBrains.Annotations.CanBeNullAttribute");
		violation.GetMessage().Should().Contain("blocks returned invocation");
	}

	[Fact]
	public async Task ParentReturnValuePolicy_AppliesToNestedLayerAndOuterFailureWins()
	{
		const string source = """
			namespace Shop.Application.Orders;

			public sealed class Pizza { }

			public sealed class PizzaOrderService
			{
				public Pizza GetPizza() => null!;
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Assembly exactName="TestAssembly" />
			    <ReturnValuePolicy description="Outer methods do not return null.">
			      <Literal value="null" />
			    </ReturnValuePolicy>
			    <Layer name="Orders">
			      <Namespace startsWith="Shop.Application.Orders" />
			      <ReturnValuePolicy description="Order methods do not return null.">
			        <Literal value="null" />
			      </ReturnValuePolicy>
			    </Layer>
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ReturnValuePolicyViolation).Subject;
		violation.Properties[ArchitecturalDiagnostics.PropertyCallerLayerName].Should().Be("Application/Orders");
		violation.GetMessage().Should().Contain("layer 'Application'");
	}

	[Theory]
	[InlineData("""<ReturnValuePolicy />""")]
	[InlineData("""<ReturnValuePolicy disallowExplicitNull="true"><Literal value="null" /></ReturnValuePolicy>""")]
	[InlineData("""<ReturnValuePolicy><Throw /></ReturnValuePolicy>""")]
	[InlineData("""<ReturnValuePolicy><Literal value="null" unsupported="pizza" /></ReturnValuePolicy>""")]
	public async Task InvalidReturnValuePolicy_ReportsConfigurationIssue(string policy)
	{
		var config = $"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			    {policy}
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public sealed class PizzaService { public object GetPizza() => null!; }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ReturnValuePolicyViolation);
	}

	[Fact]
	public async Task ConfigurationWithoutReturnValuePolicy_RemainsUnchanged()
	{
		const string source = """
			public sealed class PizzaService
			{
				public object GetPizza() => null!;
			}
			""";
		const string config = """
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class endsWith="Service" />
			  </Layer>
			</ArchitecturalLevels>
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.ReturnValuePolicyViolation);
	}
}
