using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Symbols;

internal static class ITypeSymbolAccessModifierExtensions
{
	internal static bool HasAccessModifier(this ITypeSymbol symbol, string value)
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

	private static bool MatchesSingleModifier(this ITypeSymbol symbol, string token)
	{
		var result = token.ToLowerInvariant() switch
		{
			"public" => symbol.DeclaredAccessibility == Accessibility.Public,
			"internal" => symbol.DeclaredAccessibility == Accessibility.Internal,
			"private" => symbol.DeclaredAccessibility == Accessibility.Private,
			"protected" => symbol.DeclaredAccessibility == Accessibility.Protected,
			"sealed" => symbol.IsSealed,
			"abstract" => symbol.IsAbstract,
			"static" => symbol.IsStatic,
			"record" => symbol.IsRecord,
			_ => false
		};

		return result;
	}
}
