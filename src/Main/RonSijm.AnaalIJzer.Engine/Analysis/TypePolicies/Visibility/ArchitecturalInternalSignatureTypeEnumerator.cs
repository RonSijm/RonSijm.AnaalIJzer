using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Visibility;

internal static class ArchitecturalInternalSignatureTypeEnumerator
{
    internal static IEnumerable<(ITypeSymbol Type, string Site)> Enumerate(ISymbol symbol)
    {
        switch (symbol)
        {
            case INamedTypeSymbol namedType:
                if (namedType.BaseType is { SpecialType: SpecialType.None } baseType)
                {
                    yield return (baseType, DependencySites.Inheritance);
                }
                foreach (var interfaceType in namedType.Interfaces)
                {
                    yield return (interfaceType, DependencySites.InterfaceImplementation);
                }
                foreach (var typeParameter in namedType.TypeParameters)
                {
                    foreach (var constraint in typeParameter.ConstraintTypes)
                    {
                        yield return (constraint, DependencySites.GenericArgument);
                    }
                }
                break;
            case IMethodSymbol method:
                var parameterSite = method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor
                    ? DependencySites.Constructor
                    : DependencySites.Method;
                foreach (var parameter in method.Parameters)
                {
                    yield return (parameter.Type, parameterSite);
                }
                if (!method.ReturnsVoid && method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor))
                {
                    yield return (method.ReturnType, DependencySites.MethodReturn);
                }
                foreach (var typeParameter in method.TypeParameters)
                {
                    foreach (var constraint in typeParameter.ConstraintTypes)
                    {
                        yield return (constraint, DependencySites.GenericArgument);
                    }
                }
                break;
            case IPropertySymbol property:
                yield return (property.Type, DependencySites.Property);
                foreach (var parameter in property.Parameters)
                {
                    yield return (parameter.Type, DependencySites.Property);
                }
                break;
            case IFieldSymbol field:
                yield return (field.Type, DependencySites.Field);
                break;
            case IEventSymbol eventSymbol:
                yield return (eventSymbol.Type, DependencySites.Field);
                break;
        }
    }

    internal static IEnumerable<INamedTypeSymbol> Expand(ITypeSymbol type)
    {
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var stack = new Stack<ITypeSymbol>();
        stack.Push(type);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current))
            {
                continue;
            }

            switch (current)
            {
                case IArrayTypeSymbol array:
                    stack.Push(array.ElementType);
                    continue;
                case IPointerTypeSymbol pointer:
                    stack.Push(pointer.PointedAtType);
                    continue;
                case INamedTypeSymbol namedType:
                    yield return namedType;
                    for (var index = namedType.TypeArguments.Length - 1; index >= 0; index--)
                    {
                        stack.Push(namedType.TypeArguments[index]);
                    }
                    break;
            }
        }
    }
}
