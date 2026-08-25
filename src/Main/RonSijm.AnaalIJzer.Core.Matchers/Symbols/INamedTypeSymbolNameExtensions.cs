using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Symbols;

public static class INamedTypeSymbolNameExtensions
{
	public static bool NameMatches(this INamedTypeSymbol symbol, string value)
	{
		var result = string.Equals(symbol.Name, value, StringComparison.Ordinal)
		             || string.Equals(symbol.ToDisplayString(), value, StringComparison.Ordinal);

		return result;
	}
}
