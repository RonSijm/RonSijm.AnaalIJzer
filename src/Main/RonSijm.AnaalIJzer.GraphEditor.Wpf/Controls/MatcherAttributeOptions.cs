using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal static class MatcherAttributeOptions
{
	public static ImmutableArray<string> GetNames(string? elementKind)
	{
		var profile = string.Equals(elementKind, "Class", StringComparison.Ordinal)
			? MatcherAttributeProfile.Type
			: MatcherAttributeProfile.NamespaceOrAssembly;
		var result = MatcherAttributeCatalog.GetAttributeNames(profile);

		return result;
	}
}
