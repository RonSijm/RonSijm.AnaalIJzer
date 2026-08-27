using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Matchers.Symbols;

public static class NamedTypeSymbolNameExtensions
{
	public static bool NameMatches(this INamedTypeSymbol symbol, string value)
	{
		var result = string.Equals(symbol.Name, value, StringComparison.Ordinal)
		             || string.Equals(symbol.ToDisplayString(), value, StringComparison.Ordinal);

		return result;
	}
}
