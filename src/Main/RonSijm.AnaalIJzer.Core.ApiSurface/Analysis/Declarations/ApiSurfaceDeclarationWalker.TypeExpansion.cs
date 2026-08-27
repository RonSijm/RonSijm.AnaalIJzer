using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Declarations;

internal static partial class ApiSurfaceDeclarationWalker
{
	private static IEnumerable<ApiSurfaceTypeReference> ExpandType(ITypeSymbol type, Location location, string site)
	{
		var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		foreach (var reference in ExpandType(type, location, site, seen))
		{
			yield return reference;
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> ExpandType(ITypeSymbol type, TypeSyntax syntax, string site)
	{
		var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		foreach (var reference in ExpandType(type, syntax, site, seen))
		{
			yield return reference;
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> ExpandType(ITypeSymbol type, TypeSyntax syntax, string site, ISet<ITypeSymbol> seen)
	{
		if (!seen.Add(type))
		{
			yield break;
		}

		switch (type)
		{
			case IArrayTypeSymbol arrayType when syntax is ArrayTypeSyntax arraySyntax:
				foreach (var reference in ExpandType(arrayType.ElementType, arraySyntax.ElementType, site, seen))
				{
					yield return reference;
				}
				yield break;
			case IPointerTypeSymbol pointerType when syntax is PointerTypeSyntax pointerSyntax:
				foreach (var reference in ExpandType(pointerType.PointedAtType, pointerSyntax.ElementType, site, seen))
				{
					yield return reference;
				}
				yield break;
			case INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType when syntax is NullableTypeSyntax nullableSyntax:
				foreach (var reference in ExpandType(nullableType.TypeArguments[0], nullableSyntax.ElementType, DependencySites.GenericArgument, seen))
				{
					yield return reference;
				}
				yield break;
		}

		if (type is not INamedTypeSymbol namedType)
		{
			foreach (var reference in ExpandType(type, syntax.GetLocation(), site, seen))
			{
				yield return reference;
			}
			yield break;
		}

		yield return new ApiSurfaceTypeReference(namedType.OriginalDefinition, site, syntax.GetLocation());
		var typeArgumentSyntax = GetTypeArgumentSyntax(syntax);
		for (var index = 0; index < namedType.TypeArguments.Length; index++)
		{
			var argumentSyntax = index < typeArgumentSyntax.Length ? typeArgumentSyntax[index] : syntax;
			foreach (var reference in ExpandType(namedType.TypeArguments[index], argumentSyntax, DependencySites.GenericArgument, seen))
			{
				yield return reference;
			}
		}

		if (namedType.IsTupleType && syntax is TupleTypeSyntax tupleSyntax)
		{
			for (var index = 0; index < namedType.TupleElements.Length && index < tupleSyntax.Elements.Count; index++)
			{
				foreach (var reference in ExpandType(namedType.TupleElements[index].Type, tupleSyntax.Elements[index].Type, DependencySites.GenericArgument, seen))
				{
					yield return reference;
				}
			}
		}

		if (namedType is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invokeMethod })
		{
			foreach (var reference in ExpandType(invokeMethod.ReturnType, syntax.GetLocation(), DependencySites.GenericArgument, seen))
			{
				yield return reference;
			}
			foreach (var parameter in invokeMethod.Parameters)
			{
				foreach (var reference in ExpandType(parameter.Type, syntax.GetLocation(), DependencySites.GenericArgument, seen))
				{
					yield return reference;
				}
			}
		}
	}

	private static ImmutableArray<TypeSyntax> GetTypeArgumentSyntax(TypeSyntax syntax)
	{
		var genericName = syntax switch
		{
			GenericNameSyntax direct => direct,
			QualifiedNameSyntax { Right: GenericNameSyntax qualified } => qualified,
			AliasQualifiedNameSyntax { Name: GenericNameSyntax aliased } => aliased,
			_ => null
		};
		var result = genericName?.TypeArgumentList.Arguments.ToImmutableArray() ?? ImmutableArray<TypeSyntax>.Empty;

		return result;
	}

	private static IEnumerable<ApiSurfaceTypeReference> ExpandType(ITypeSymbol type, Location location, string site, ISet<ITypeSymbol> seen)
	{
		if (!seen.Add(type))
		{
			yield break;
		}

		switch (type)
		{
			case IArrayTypeSymbol arrayType:
				foreach (var reference in ExpandType(arrayType.ElementType, location, site, seen))
				{
					yield return reference;
				}
				yield break;
			case IPointerTypeSymbol pointerType:
				foreach (var reference in ExpandType(pointerType.PointedAtType, location, site, seen))
				{
					yield return reference;
				}
				yield break;
			case IFunctionPointerTypeSymbol functionPointer:
				foreach (var reference in ExpandType(functionPointer.Signature.ReturnType, location, DependencySites.GenericArgument, seen))
				{
					yield return reference;
				}
				foreach (var parameter in functionPointer.Signature.Parameters)
				{
					foreach (var reference in ExpandType(parameter.Type, location, DependencySites.GenericArgument, seen))
					{
						yield return reference;
					}
				}
				yield break;
		}

		if (type is not INamedTypeSymbol namedType)
		{
			yield break;
		}

		yield return new ApiSurfaceTypeReference(namedType.OriginalDefinition, site, location);
		foreach (var typeArgument in namedType.TypeArguments)
		{
			foreach (var reference in ExpandType(typeArgument, location, DependencySites.GenericArgument, seen))
			{
				yield return reference;
			}
		}

		if (namedType is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invokeMethod })
		{
			foreach (var reference in ExpandType(invokeMethod.ReturnType, location, DependencySites.GenericArgument, seen))
			{
				yield return reference;
			}
			foreach (var parameter in invokeMethod.Parameters)
			{
				foreach (var reference in ExpandType(parameter.Type, location, DependencySites.GenericArgument, seen))
				{
					yield return reference;
				}
			}
		}
	}
}
