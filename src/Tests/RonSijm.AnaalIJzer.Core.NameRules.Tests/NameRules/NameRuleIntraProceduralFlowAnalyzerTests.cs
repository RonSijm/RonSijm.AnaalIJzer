using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.NameRules;

namespace RonSijm.AnaalIJzer.Core.NameRules.Tests.NameRules;

public sealed class NameRuleIntraProceduralFlowAnalyzerTests
{
    [Fact]
    public void Analyze_TracksAnUnambiguousLocalAliasIntoAnInvocation()
    {
        var (graph, method) = CreateGraph("""
			class OrderService
			{
				void Run(int animalId)
				{
					var alias = animalId;
					Save(alias);
				}

				void Save(int fruitId) { }
			}
			""");

        var flows = NameRuleIntraProceduralFlowAnalyzer.Analyze(graph, method);

        var flow = flows.Should().ContainSingle().Which;
        flow.Source.DisplayName.Should().Be("animalId");
        flow.Target.DisplayName.Should().Be("fruitId");
        flow.Site.Should().Be("Method");
    }

    [Fact]
    public void Analyze_TracksAnUnambiguousLocalAliasIntoAReturn()
    {
        var (graph, method) = CreateGraph("""
			class OrderService
			{
				int GetFruitId(int animalId)
				{
					var alias = animalId;
					return alias;
				}
			}
			""");

        var flows = NameRuleIntraProceduralFlowAnalyzer.Analyze(graph, method);

        var flow = flows.Should().ContainSingle().Which;
        flow.Source.DisplayName.Should().Be("animalId");
        flow.Target.DisplayName.Should().Be("GetFruitId");
        flow.Site.Should().Be("MethodReturn");
    }

    [Fact]
    public void Analyze_TracksALongLinearLocalAliasChain()
    {
        var aliases = string.Join(Environment.NewLine, Enumerable.Range(1, 256).Select(index => $"var value{index} = value{index - 1};"));
        var (graph, method) = CreateGraph($$"""
			class OrderService
			{
				void Run(int customerId)
				{
					var value0 = customerId;
					{{aliases}}
					Save(value256);
				}

				void Save(int orderId) { }
			}
			""");

        var flows = NameRuleIntraProceduralFlowAnalyzer.Analyze(graph, method);

        flows.Should().Contain(flow => flow.Source.DisplayName == "customerId" && flow.Target.DisplayName == "orderId" && flow.Site == "Method");
    }

    [Fact]
    public void Analyze_TracksAnUnambiguousCapturedParameterAliasInsideALambda()
    {
        const string source = """
			using System;

			class OrderService
			{
				void Run(int animalId)
				{
					Action save = () =>
					{
						var alias = animalId;
						Save(alias);
					};
				}

				void Save(int fruitId) { }
			}
			""";
        var cancellationToken = TestContext.Current.CancellationToken;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var lambdaSyntax = syntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedLambdaExpressionSyntax>().Single();
        var lambda = (IAnonymousFunctionOperation)semanticModel.GetOperation(lambdaSyntax, cancellationToken)!;

        var flows = NameRuleIntraProceduralFlowAnalyzer.AnalyzeLinearly(lambda.Body, lambda.Symbol);

        flows.Should().Contain(flow => flow.Source.DisplayName == "animalId" && flow.Target.DisplayName == "fruitId" && flow.Site == "Method");
    }

    private static (ControlFlowGraph Graph, IMethodSymbol Method) CreateGraph(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
        var method = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodDeclaration)!;
        var operation = (IBlockOperation)semanticModel.GetOperation(methodDeclaration.Body!)!;
        var methodBody = operation.Parent.Should().BeAssignableTo<IMethodBodyOperation>().Which;
        var graph = ControlFlowGraph.Create(methodBody);

        var result = (graph, method);

        return result;
    }
}