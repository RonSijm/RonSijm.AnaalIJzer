using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RonSijm.AnaalIJzer.Analysis.ApiSurface.Model;

namespace RonSijm.AnaalIJzer.Analysis.ApiSurface.Declarations;

internal static partial class ApiSurfaceDeclarationWalker
{
	private static IEnumerable<ApiSurfaceTypeReference> GetNamedTypeReferences(INamedTypeSymbol type, Compilation compilation, CancellationToken cancellationToken)
	{
		var declarations = type.DeclaringSyntaxReferences
			.Select(reference => reference.GetSyntax(cancellationToken))
			.OrderBy(syntax => syntax.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(syntax => syntax.SpanStart)
			.ToArray();
		if (type.TypeKind == TypeKind.Delegate && type.DelegateInvokeMethod is { } invokeMethod)
		{
			var delegateSyntax = declarations.OfType<DelegateDeclarationSyntax>().FirstOrDefault();
			if (delegateSyntax is not null)
			{
				foreach (var reference in ExpandType(invokeMethod.ReturnType, delegateSyntax.ReturnType, DependencySites.MethodReturn))
				{
					yield return reference;
				}

				foreach (var reference in GetParameterReferences(invokeMethod.Parameters, delegateSyntax.ParameterList.Parameters, DependencySites.Method))
				{
					yield return reference;
				}
			}
		}

		foreach (var baseType in GetBaseTypes(type))
		{
			var syntax = FindBaseTypeSyntax(declarations, baseType.Type, compilation, cancellationToken);
			if (syntax is null)
			{
				continue;
			}

			foreach (var reference in ExpandType(baseType.Type, syntax.Type, baseType.Site))
			{
				yield return reference;
			}
		}
	}

	private static IEnumerable<(INamedTypeSymbol Type, string Site)> GetBaseTypes(INamedTypeSymbol type)
	{
		if (type.BaseType is { SpecialType: SpecialType.None } baseType)
		{
			yield return (baseType, DependencySites.Inheritance);
		}

		foreach (var interfaceType in type.Interfaces)
		{
			yield return (interfaceType, DependencySites.InterfaceImplementation);
		}
	}

	private static BaseTypeSyntax? FindBaseTypeSyntax(IEnumerable<SyntaxNode> declarations, INamedTypeSymbol expectedType, Compilation compilation, CancellationToken cancellationToken)
	{
		foreach (var baseTypeSyntax in declarations.OfType<TypeDeclarationSyntax>().SelectMany(declaration => declaration.BaseList?.Types ?? []))
		{
			var semanticModel = compilation.GetSemanticModel(baseTypeSyntax.SyntaxTree);
			var actualType = semanticModel.GetTypeInfo(baseTypeSyntax.Type, cancellationToken).Type as INamedTypeSymbol;
			if (actualType is not null && SymbolEqualityComparer.Default.Equals(actualType.OriginalDefinition, expectedType.OriginalDefinition))
			{
				return baseTypeSyntax;
			}
		}

		return null;
	}
}
