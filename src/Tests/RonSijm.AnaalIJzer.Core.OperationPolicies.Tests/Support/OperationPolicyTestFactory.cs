using RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Tests.Support;

internal static class OperationPolicyTestFactory
{
	internal static BehavioralOperationBodyAnalysis GetBehavioralBodyAnalysis(string source, string methodName)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var compilation = CSharpCompilation.Create(
			"OperationPolicyTests",
			[syntaxTree],
			TrustedPlatformReferences.Value,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var semanticModel = compilation.GetSemanticModel(syntaxTree);
		var method = syntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<MethodDeclarationSyntax>()
			.Single(node => node.Identifier.ValueText == methodName);
		var symbol = semanticModel.GetDeclaredSymbol(method);
		symbol.Should().NotBeNull();
		var operation = semanticModel.GetOperation(method.Body!);
		operation.Should().NotBeNull();
		var created = BehavioralOperationBodyAnalysis.TryCreate(operation!, symbol!, out var analysis);
		created.Should().BeTrue();

		return analysis;
	}

	internal static SemanticOperation GetOperation<TSyntax>(string source, Func<TSyntax, bool>? predicate = null)
		where TSyntax : SyntaxNode
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var compilation = CSharpCompilation.Create(
			"OperationPolicyTests",
			[syntaxTree],
			TrustedPlatformReferences.Value,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var semanticModel = compilation.GetSemanticModel(syntaxTree);
		var syntax = syntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<TSyntax>()
			.Single(node => predicate?.Invoke(node) ?? true);
		var created = SemanticOperationFactory.TryCreate(semanticModel.GetOperation(syntax), semanticModel, CancellationToken.None, out var operation);
		created.Should().BeTrue();

		return operation;
	}

	private static readonly Lazy<MetadataReference[]> TrustedPlatformReferences = new(() =>
	{
		var result = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToArray();

		return result;
	});
}
