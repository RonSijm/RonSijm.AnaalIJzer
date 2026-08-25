using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Symbols;

// ReSharper disable once InconsistentNaming - Justification: Extension for ITypeSymbol
public static class ITypeSymbolInterfaceExtensions
{
	public static bool ImplementsInterface(this ITypeSymbol symbol, string value)
	{
		var result = false;
		foreach (var interfaceType in symbol.AllInterfaces)
		{
			if (interfaceType.NameMatches(value))
			{
				result = true;
				break;
			}
		}

		return result;
	}
}
