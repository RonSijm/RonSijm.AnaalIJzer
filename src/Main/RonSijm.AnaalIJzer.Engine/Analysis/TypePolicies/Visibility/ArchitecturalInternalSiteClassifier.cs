using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Visibility;

internal static class ArchitecturalInternalSiteClassifier
{
    internal static string GetSite(SyntaxNode node, ISymbol referencedSymbol)
    {
        if (node.FirstAncestorOrSelf<AttributeSyntax>() is not null)
        {
            return DependencySites.Attribute;
        }

        if (node.FirstAncestorOrSelf<BaseTypeSyntax>() is not null)
        {
            var result = referencedSymbol is INamedTypeSymbol { TypeKind: TypeKind.Interface }
                ? DependencySites.InterfaceImplementation
                : DependencySites.Inheritance;

            return result;
        }

        if (node.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>() is not null
            || node.FirstAncestorOrSelf<ImplicitObjectCreationExpressionSyntax>() is not null)
        {
            return DependencySites.New;
        }

        if (node.FirstAncestorOrSelf<TypeArgumentListSyntax>() is not null)
        {
            return DependencySites.GenericArgument;
        }

        if (node.FirstAncestorOrSelf<FieldDeclarationSyntax>() is not null
            || node.FirstAncestorOrSelf<EventFieldDeclarationSyntax>() is not null
            || node.FirstAncestorOrSelf<EventDeclarationSyntax>() is not null)
        {
            return DependencySites.Field;
        }

        if (node.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is not null
            || node.FirstAncestorOrSelf<IndexerDeclarationSyntax>() is not null)
        {
            return DependencySites.Property;
        }

        if (node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>() is not null)
        {
            return DependencySites.Local;
        }

        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is not null && method.ReturnType.Span.Contains(node.Span))
        {
            return DependencySites.MethodReturn;
        }

        if (node.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is not null)
        {
            return DependencySites.Constructor;
        }

        if (referencedSymbol.IsStatic && node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>() is not null)
        {
            return DependencySites.StaticMember;
        }

        return DependencySites.Method;
    }
}
