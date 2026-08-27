using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ApiSurface;

internal static partial class ApiSurfaceAnalyzer
{
	private static bool CanTraverse(INamedTypeSymbol type)
	{
		var result = type.SpecialType == SpecialType.None
		             && type.TypeKind != TypeKind.Enum
		             && !type.IsUnboundGenericType;

		return result;
	}

	private static ISymbol NormalizePartialSymbol(ISymbol symbol)
	{
		if (symbol is IMethodSymbol method && method.PartialDefinitionPart is not null)
		{
			return method.PartialDefinitionPart;
		}

		return symbol;
	}

	private static INamedTypeSymbol? GetPolicyOwnerType(ISymbol symbol)
	{
		var result = symbol switch
		{
			INamedTypeSymbol namedType => namedType,
			_ => symbol.ContainingType
		};

		return result;
	}

	private static string GetDisplayName(ISymbol symbol)
	{
		var result = symbol is INamedTypeSymbol
			? symbol.Name
			: $"{symbol.ContainingType?.Name}.{symbol.Name}";

		return result;
	}
}
