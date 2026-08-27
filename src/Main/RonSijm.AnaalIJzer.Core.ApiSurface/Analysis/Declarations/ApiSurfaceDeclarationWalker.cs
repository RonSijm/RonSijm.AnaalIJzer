using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Model;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.ApiSurface.Analysis.Declarations;

internal static partial class ApiSurfaceDeclarationWalker
{
	internal static IEnumerable<ApiSurfaceTypeReference> GetReferences(ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
	{
		foreach (var reference in GetSignatureReferences(symbol, compilation, cancellationToken))
		{
			yield return reference;
		}

		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass is null || attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax syntax)
			{
				continue;
			}

			foreach (var reference in ExpandType(attribute.AttributeClass, syntax.Name.GetLocation(), DependencySites.Attribute))
			{
				yield return reference;
			}
		}
	}

	private static IEnumerable<ApiSurfaceTypeReference> GetSignatureReferences(ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
	{
		switch (symbol)
		{
			case INamedTypeSymbol type:
				foreach (var reference in GetNamedTypeReferences(type, compilation, cancellationToken))
				{
					yield return reference;
				}
				break;
			case IMethodSymbol method when IsSupportedMethod(method):
				foreach (var reference in GetMethodReferences(method, cancellationToken))
				{
					yield return reference;
				}
				break;
			case IPropertySymbol property:
				foreach (var reference in GetPropertyReferences(property, cancellationToken))
				{
					yield return reference;
				}
				break;
			case IFieldSymbol field:
				foreach (var reference in GetFieldReferences(field, cancellationToken))
				{
					yield return reference;
				}
				break;
			case IEventSymbol eventSymbol:
				foreach (var reference in GetEventReferences(eventSymbol, cancellationToken))
				{
					yield return reference;
				}
				break;
		}
	}

	private static bool IsSupportedMethod(IMethodSymbol method)
	{
		var result = method.MethodKind is MethodKind.Ordinary
			or MethodKind.Constructor
			or MethodKind.StaticConstructor
			or MethodKind.UserDefinedOperator
			or MethodKind.Conversion;

		return result;
	}
}
