using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Matchers.Symbols;

// ReSharper disable once InconsistentNaming - Justification: Extension for ITypeSymbol
public static class ITypeSymbolTypeKindExtension
{
	public static bool HasTypeKind(this ITypeSymbol symbol, string value)
	{
		var result = value.Trim().ToLowerInvariant() switch
		{
			"class" => symbol is { TypeKind: TypeKind.Class, IsRecord: false },
			"interface" => symbol is { TypeKind: TypeKind.Interface },
			"struct" => symbol is { TypeKind: TypeKind.Struct, IsRecord: false },
			"record" => symbol is { TypeKind: TypeKind.Class, IsRecord: true },
			"recordstruct" => symbol is { TypeKind: TypeKind.Struct, IsRecord: true },
			"enum" => symbol is { TypeKind: TypeKind.Enum },
			"delegate" => symbol is { TypeKind: TypeKind.Delegate },
			_ => false
		};

		return result;
	}

	public static bool IsSupportedTypeKind(string value)
	{
		var result = value.Trim().ToLowerInvariant() is "class" or "interface" or "struct" or "record" or "recordstruct" or "enum" or "delegate";

		return result;
	}
}
