using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Matchers.Symbols;

public static class TypeSymbolAccessModifierExtensions
{
	public static bool HasAccessModifier(this ISymbol symbol, string value)
	{
		var result = true;
		foreach (var token in value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
		{
			if (!symbol.MatchesSingleModifier(token))
			{
				result = false;
				break;
			}
		}

		return result;
	}

	private static bool MatchesSingleModifier(this ISymbol symbol, string token)
	{
		var result = token.ToLowerInvariant() switch
		{
			"public" => symbol.DeclaredAccessibility == Accessibility.Public,
			"internal" => symbol.DeclaredAccessibility == Accessibility.Internal,
			"private" => symbol.DeclaredAccessibility == Accessibility.Private,
			"protected" => symbol.DeclaredAccessibility == Accessibility.Protected,
			"sealed" => symbol is INamedTypeSymbol { IsSealed: true } or IMethodSymbol { IsSealed: true },
			"abstract" => symbol is INamedTypeSymbol { IsAbstract: true } or IMethodSymbol { IsAbstract: true },
			"static" => symbol is INamedTypeSymbol { IsStatic: true }
			            or IMethodSymbol { IsStatic: true }
			            or IPropertySymbol { IsStatic: true }
			            or IFieldSymbol { IsStatic: true }
			            or IEventSymbol { IsStatic: true },
			"record" => symbol is INamedTypeSymbol { IsRecord: true },
			_ => false
		};

		return result;
	}
}
