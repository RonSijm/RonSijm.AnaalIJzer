using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RonSijm.AnaalIJzer.Symbols;

internal static class StringMatcherExtensions
{
	private static readonly ConcurrentDictionary<string, Regex?> RegexCache = new(StringComparer.Ordinal);

	internal static string ToFullName(this string typeName, string namespaceName)
	{
		var result = string.IsNullOrEmpty(namespaceName) ? typeName : namespaceName + "." + typeName;

		return result;
	}

	internal static bool MatchesRegexPattern(this string subject, string pattern)
	{
		var regex = RegexCache.GetOrAdd(pattern, static p =>
		{
			try
			{
				return new Regex(p, RegexOptions.CultureInvariant);
			}
			catch (ArgumentException)
			{
				return null;
			}
		});

		var result = regex is not null && regex.IsMatch(subject);

		return result;
	}
}
