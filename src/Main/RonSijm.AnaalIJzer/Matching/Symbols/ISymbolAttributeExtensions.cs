using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Symbols;

internal static class ISymbolAttributeExtensions
{
	internal static bool HasAttribute(this ISymbol symbol, string value)
	{
		var normalised = value.EndsWith("Attribute", StringComparison.Ordinal) ? value : value + "Attribute";
		var result = false;
		foreach (var attr in symbol.GetAttributes())
		{
			var cls = attr.AttributeClass;
			if (cls is null)
			{
				continue;
			}

			if (string.Equals(cls.Name, normalised, StringComparison.Ordinal)
			    || string.Equals(cls.Name, value, StringComparison.Ordinal)
			    || string.Equals(cls.ToDisplayString(), value, StringComparison.Ordinal))
			{
				result = true;
				break;
			}
		}

		return result;
	}
}
