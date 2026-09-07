using System.Globalization;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Formatting;

internal static class AssemblyAttributeArgumentValueFormatter
{
	internal static string Format(TypedConstant value)
	{
		if (value.IsNull)
		{
			return "null";
		}

		if (value.Kind == TypedConstantKind.Array)
		{
			var values = value.Values.Select(Format);
			var arrayResult = "[" + string.Join(", ", values) + "]";

			return arrayResult;
		}

		if (value.Kind == TypedConstantKind.Type && value.Value is ITypeSymbol typeSymbol)
		{
			var typeResult = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);

			return typeResult;
		}

		if (value.Value is bool boolean)
		{
			var booleanResult = boolean ? "true" : "false";

			return booleanResult;
		}

		if (value.Value is IFormattable formattable)
		{
			var formattedResult = formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

			return formattedResult;
		}

		var result = value.Value?.ToString() ?? string.Empty;

		return result;
	}
}
