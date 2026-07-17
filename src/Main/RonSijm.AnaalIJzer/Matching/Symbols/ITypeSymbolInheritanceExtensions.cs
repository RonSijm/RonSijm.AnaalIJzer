using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Symbols;

// ReSharper disable once InconsistentNaming - Justification: Extension for ITypeSymbol
internal static class ITypeSymbolInheritanceExtensions
{
	internal static bool InheritsFrom(this ITypeSymbol symbol, string value)
	{
		var result = false;
		for (var baseType = symbol.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			if (baseType.NameMatches(value))
			{
				result = true;
				break;
			}
		}

		return result;
	}
}
