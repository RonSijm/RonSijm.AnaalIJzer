using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Observations;

public static class CompilationTypeCollector
{
    public static IReadOnlyList<INamedTypeSymbol> GetProjectTypes(Compilation compilation, CancellationToken cancellationToken)
    {
        var result = GetProjectTypes(compilation, GeneratedCodeAnalysisScope.Exclude, cancellationToken);

        return result;
    }

    public static IReadOnlyList<INamedTypeSymbol> GetProjectTypes(Compilation compilation, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
    {
        var types = new List<INamedTypeSymbol>();
        CollectNamespaceTypes(compilation.Assembly.GlobalNamespace, types, generatedCodeScope, cancellationToken);
        var result = DistinctTypes(types)
            .OrderBy(type => type.Locations.FirstOrDefault(location => location.IsInSource)?.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.Locations.FirstOrDefault(location => location.IsInSource)?.SourceSpan.Start ?? int.MaxValue)
            .ToArray();

        return result;
    }

    private static void CollectNamespaceTypes(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> types, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            CollectType(type, types, generatedCodeScope, cancellationToken);
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            CollectNamespaceTypes(childNamespace, types, generatedCodeScope, cancellationToken);
        }
    }

    private static IEnumerable<INamedTypeSymbol> DistinctTypes(IEnumerable<INamedTypeSymbol> types)
    {
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var type in types)
        {
            if (seen.Add(type))
            {
                yield return type;
            }
        }
    }

    private static void CollectType(INamedTypeSymbol type, List<INamedTypeSymbol> types, GeneratedCodeAnalysisScope generatedCodeScope, CancellationToken cancellationToken)
    {
        if (type.DeclaringSyntaxReferences.Any(reference => generatedCodeScope.ShouldAnalyze(reference.SyntaxTree, cancellationToken)))
        {
            types.Add(type.OriginalDefinition);
        }

        foreach (var nestedType in type.GetTypeMembers())
        {
            CollectType(nestedType, types, generatedCodeScope, cancellationToken);
        }
    }
}