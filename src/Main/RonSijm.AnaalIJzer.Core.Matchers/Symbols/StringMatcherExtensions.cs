using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RonSijm.AnaalIJzer.Symbols;

internal static class StringMatcherExtensions
{
	private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex?> RegexCache = new();

	internal static string ToFullName(this string typeName, string namespaceName)
	{
		var result = string.IsNullOrEmpty(namespaceName) ? typeName : namespaceName + "." + typeName;

		return result;
	}

	internal static bool MatchesRegexPattern(this string subject, string pattern, RegexOptions options)
	{
		var regex = RegexCache.GetOrAdd((pattern, options), static entry =>
		{
			try
			{
				return new Regex(entry.Pattern, entry.Options);
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
