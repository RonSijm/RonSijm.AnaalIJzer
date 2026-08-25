using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Engine.NameRules;

public static class NameRuleSubjectFactory
{
	public static NameRuleSubject? CreateType(ITypeSymbol type)
	{
		var comparisonType = type;
		if (comparisonType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType
		    && nullableType.TypeArguments.Length == 1)
		{
			comparisonType = nullableType.TypeArguments[0];
		}

		if (comparisonType.SpecialType == SpecialType.System_Void
		    || comparisonType.TypeKind is TypeKind.Error or TypeKind.TypeParameter
		    || comparisonType.IsAnonymousType)
		{
			return null;
		}

		var displayName = comparisonType switch
		{
			IArrayTypeSymbol array => array.ElementType.Name + "[]",
			_ => comparisonType.Name
		};
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return null;
		}

		var namespaceName = comparisonType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
		var result = new NameRuleSubject(NameRuleSubjectKind.TypeName, displayName, [displayName], namespaceName, comparisonType);

		return result;
	}

	public static NameRuleSubject CreateDeclarationName(string declaredName, ITypeSymbol type)
	{
		var result = new NameRuleSubject(NameRuleSubjectKind.ValueName, declaredName, [declaredName], type.ContainingNamespace?.ToDisplayString() ?? string.Empty, type);

		return result;
	}
}
