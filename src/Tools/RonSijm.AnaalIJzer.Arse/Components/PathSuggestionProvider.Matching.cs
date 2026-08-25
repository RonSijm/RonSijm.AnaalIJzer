namespace RonSijm.AnaalIJzer.Arse.Components;

internal static partial class PathSuggestionProvider
{
	private static HashSet<string> NormalizeExtensions(IReadOnlyCollection<string>? extensions)
	{
		if (extensions is null || extensions.Count == 0)
		{
			return [];
		}

		var result = extensions
			.Select(extension => extension.StartsWith('.') ? extension : "." + extension)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return result;
	}

	private static bool MatchesExtension(string path, HashSet<string> extensions)
	{
		var result = extensions.Count == 0 || extensions.Contains(Path.GetExtension(path));

		return result;
	}

	private static string LongestCommonPrefix(IEnumerable<string> values)
	{
		using var enumerator = values.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			return string.Empty;
		}

		var prefix = enumerator.Current;
		while (enumerator.MoveNext())
		{
			var value = enumerator.Current;
			var length = 0;
			while (length < prefix.Length && length < value.Length && char.ToUpperInvariant(prefix[length]) == char.ToUpperInvariant(value[length]))
			{
				length++;
			}

			prefix = prefix[..length];
			if (prefix.Length == 0)
			{
				break;
			}
		}

		var result = prefix;

		return result;
	}
}
