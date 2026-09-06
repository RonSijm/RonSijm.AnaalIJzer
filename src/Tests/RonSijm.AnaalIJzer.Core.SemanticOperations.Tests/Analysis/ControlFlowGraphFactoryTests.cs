using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Tests.Analysis;

public sealed class ControlFlowGraphFactoryTests
{
	[Fact]
	public void TryCreate_ClimbsFromAMethodBlockToItsMethodBodyRoot()
	{
		const string source = """
			public sealed class CandyService
			{
				public void Prepare(int candyId)
				{
					var orderId = candyId;
				}
			}
			""";
		var cancellationToken = TestContext.Current.CancellationToken;
		var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
		var compilation = CSharpCompilation.Create(
			"ControlFlowGraphFactoryTests",
			[syntaxTree],
			((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path)),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var method = syntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
		var block = (IBlockOperation)compilation.GetSemanticModel(syntaxTree).GetOperation(method.Body!, cancellationToken)!;

		var created = ControlFlowGraphFactory.TryCreate(block, out var graph);

		created.Should().BeTrue();
		graph.Blocks.Should().NotBeEmpty();
	}

}
