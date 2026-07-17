using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

internal static class MatcherAttributeOptions
{
	public static ImmutableArray<string> GetNames(string? elementKind)
	{
		var result = string.Equals(elementKind, "Class", StringComparison.Ordinal)
			? ImmutableArray.Create("endsWith", "startsWith", "typeName", "exactName", "exactFullName", "contains", "regex", "typeKind", "inherits", "implements", "withAttribute", "withAccessModifier")
			: ImmutableArray.Create("startsWith", "endsWith", "exactName", "contains", "regex");

		return result;
	}
}
