using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Visibility;

public static class SymbolAccessibilityExtensions
{
	public static bool TryGetArchitectureAccessibility(this ISymbol symbol, out ArchitectureAccessibility accessibility)
	{
		if (symbol is INamedTypeSymbol { IsFileLocal: true })
		{
			accessibility = ArchitectureAccessibility.File;
			return true;
		}

		var result = TryMapAccessibility(symbol.DeclaredAccessibility, out accessibility);

		return result;
	}

	public static bool IsEffectivelyExternallyVisible(this ISymbol symbol)
	{
		if (!IsExternallyVisible(symbol.DeclaredAccessibility))
		{
			return false;
		}

		var containingType = symbol as INamedTypeSymbol ?? symbol.ContainingType;
		while (containingType is not null)
		{
			if (!IsExternallyVisible(containingType.DeclaredAccessibility))
			{
				return false;
			}

			containingType = containingType.ContainingType;
		}

		return true;
	}

	private static bool TryMapAccessibility(Accessibility value, out ArchitectureAccessibility accessibility)
	{
		switch (value)
		{
			case Accessibility.Public:
				accessibility = ArchitectureAccessibility.Public;
				return true;
			case Accessibility.Internal:
				accessibility = ArchitectureAccessibility.Internal;
				return true;
			case Accessibility.Protected:
				accessibility = ArchitectureAccessibility.Protected;
				return true;
			case Accessibility.ProtectedOrInternal:
				accessibility = ArchitectureAccessibility.ProtectedInternal;
				return true;
			case Accessibility.ProtectedAndInternal:
				accessibility = ArchitectureAccessibility.PrivateProtected;
				return true;
			case Accessibility.Private:
				accessibility = ArchitectureAccessibility.Private;
				return true;
			default:
				accessibility = default;
				return false;
		}
	}

	private static bool IsExternallyVisible(Accessibility accessibility)
	{
		var result = accessibility is Accessibility.Public
			or Accessibility.Protected
			or Accessibility.ProtectedOrInternal;

		return result;
	}
}
