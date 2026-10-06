using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Visibility.Annotations;

public static class ArchitecturalInternalSymbolExtensions
{
    public static ImmutableArray<ArchitectureInternalRestriction> GetArchitectureInternalRestrictions(this ISymbol symbol)
    {
        var builder = ImmutableArray.CreateBuilder<ArchitectureInternalRestriction>();
        var current = Normalize(symbol);
        builder.AddRange(current.GetDeclaredArchitectureInternalRestrictions());

        var containingType = current as INamedTypeSymbol ?? current.ContainingType;
        while (containingType is not null)
        {
            if (!SymbolEqualityComparer.Default.Equals(containingType, current))
            {
                builder.AddRange(containingType.GetDeclaredArchitectureInternalRestrictions());
            }

            containingType = containingType.ContainingType;
        }

        var result = builder
            .OrderBy(restriction => restriction.Scope)
            .ToImmutableArray();

        return result;
    }

    public static ImmutableArray<ArchitectureInternalRestriction> GetDeclaredArchitectureInternalRestrictions(this ISymbol symbol)
    {
        var builder = ImmutableArray.CreateBuilder<ArchitectureInternalRestriction>();
        AddRestrictions(builder, Normalize(symbol));
        var result = builder
            .OrderBy(restriction => restriction.Scope)
            .ToImmutableArray();

        return result;
    }

    public static bool HasArchitectureInternalAnnotations(this Compilation compilation)
    {
        var result = compilation.GetTypeByMetadataName(ArchitectureInternalAttributeNames.ClassInternal) is not null
                     || compilation.GetTypeByMetadataName(ArchitectureInternalAttributeNames.AssemblyInternal) is not null
                     || compilation.GetTypeByMetadataName(ArchitectureInternalAttributeNames.LayerInternal) is not null;

        return result;
    }

    public static INamedTypeSymbol? GetArchitectureOwnerType(this ISymbol? symbol)
    {
        var result = symbol switch
        {
            INamedTypeSymbol namedType => namedType,
            null => null,
            _ => symbol.ContainingType
        };

        return result;
    }

    public static bool IsInArchitectureTypeFamily(this INamedTypeSymbol? candidate, INamedTypeSymbol? owner)
    {
        if (candidate is null || owner is null)
        {
            return false;
        }

        var result = Contains(candidate, owner) || Contains(owner, candidate);

        return result;
    }

    private static void AddRestrictions(ImmutableArray<ArchitectureInternalRestriction>.Builder builder, ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeTypeName = attribute.AttributeClass?.ToDisplayString();
            if (!TryGetScope(attributeTypeName, out var scope))
            {
                continue;
            }

            var ownerType = symbol is INamedTypeSymbol namedType ? namedType : symbol.ContainingType;
            var restriction = new ArchitectureInternalRestriction(scope, symbol, ownerType, symbol.ContainingAssembly, attributeTypeName!);
            builder.Add(restriction);
        }
    }

    private static bool TryGetScope(string? attributeTypeName, out ArchitectureInternalScope scope)
    {
        switch (attributeTypeName)
        {
            case ArchitectureInternalAttributeNames.ClassInternal:
                scope = ArchitectureInternalScope.Class;
                return true;
            case ArchitectureInternalAttributeNames.AssemblyInternal:
                scope = ArchitectureInternalScope.Assembly;
                return true;
            case ArchitectureInternalAttributeNames.LayerInternal:
                scope = ArchitectureInternalScope.Layer;
                return true;
            default:
                scope = default;
                return false;
        }
    }

    private static ISymbol Normalize(ISymbol symbol)
    {
        var result = symbol switch
        {
            IMethodSymbol { ReducedFrom: { } reducedFrom } => reducedFrom.OriginalDefinition,
            IMethodSymbol method => method.OriginalDefinition,
            INamedTypeSymbol namedType => namedType.OriginalDefinition,
            _ => symbol.OriginalDefinition
        };

        return result;
    }

    private static bool Contains(INamedTypeSymbol candidate, INamedTypeSymbol expected)
    {
        var current = candidate;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, expected.OriginalDefinition))
            {
                return true;
            }

            current = current.ContainingType;
        }

        return false;
    }
}
