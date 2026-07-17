using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Symbols;

// ReSharper disable once InconsistentNaming - Justification: Extension for ITypeSymbol
internal static class ITypeSymbolTypeKindExtension
{
	internal static bool HasTypeKind(this ITypeSymbol symbol, string value)
	{
		var result = value.Trim().ToLowerInvariant() switch
		{
			"class" => symbol.TypeKind == TypeKind.Class && !symbol.IsRecord,
			"interface" => symbol.TypeKind == TypeKind.Interface,
			"struct" => symbol.TypeKind == TypeKind.Struct && !symbol.IsRecord,
			"record" => symbol.TypeKind == TypeKind.Class && symbol.IsRecord,
			"recordstruct" => symbol.TypeKind == TypeKind.Struct && symbol.IsRecord,
			"enum" => symbol.TypeKind == TypeKind.Enum,
			"delegate" => symbol.TypeKind == TypeKind.Delegate,
			_ => false
		};

		return result;
	}

	internal static bool IsSupportedTypeKind(string value)
	{
		var result = value.Trim().ToLowerInvariant() is "class" or "interface" or "struct" or "record" or "recordstruct" or "enum" or "delegate";

		return result;
	}
}
