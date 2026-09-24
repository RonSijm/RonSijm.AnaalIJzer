namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Tests.Support;

internal static class SemanticOperationTestFactory
{
    internal static SemanticOperation GetOperation<TSyntax>(string source, Func<TSyntax, bool>? predicate = null)
        where TSyntax : SyntaxNode
    {
        var (semanticModel, root) = CreateSemanticModel(source);
        var syntax = root
            .DescendantNodes()
            .OfType<TSyntax>()
            .Single(node => predicate?.Invoke(node) ?? true);
        var operation = semanticModel.GetOperation(syntax);
        var created = SemanticOperationFactory.TryCreate(operation, semanticModel, CancellationToken.None, out var semanticOperation);
        created.Should().BeTrue();

        return semanticOperation;
    }

    internal static bool TryCreateOperation<TSyntax>(string source, Func<TSyntax, bool>? predicate = null)
        where TSyntax : SyntaxNode
    {
        var (semanticModel, root) = CreateSemanticModel(source);
        var syntax = root
            .DescendantNodes()
            .OfType<TSyntax>()
            .Single(node => predicate?.Invoke(node) ?? true);
        var operation = semanticModel.GetOperation(syntax);
        var result = SemanticOperationFactory.TryCreate(operation, semanticModel, CancellationToken.None, out _);

        return result;
    }

    private static (SemanticModel SemanticModel, SyntaxNode Root) CreateSemanticModel(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "SemanticOperationTests",
            [syntaxTree],
            TrustedPlatformReferences.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();
        var result = (semanticModel, root);

        return result;
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