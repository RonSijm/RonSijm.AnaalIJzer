namespace RonSijm.AnaalIJzer.Arse.Components;

public enum PathSelectionMode
{
	File,
	Directory
}

internal sealed record PathSuggestion(string Value, string DisplayValue, bool IsDirectory);

internal sealed record PathSuggestionSet(IReadOnlyList<PathSuggestion> Suggestions, string CompletionValue)
{
	public static PathSuggestionSet Empty(string value)
	{
		var result = new PathSuggestionSet([], value);

		return result;
	}
}

internal static class PathSuggestionProvider
{
	private const int MaximumSuggestions = 8;

	public static PathSuggestionSet Find(string input, PathSelectionMode mode, IReadOnlyCollection<string>? fileExtensions = null, bool allowMultiple = false, string? workingDirectory = null)
	{
		var (inputPrefix, segment) = SplitInput(input, allowMultiple);
		if (!TryResolveSearch(segment, workingDirectory ?? Environment.CurrentDirectory, out var search))
		{
			return PathSuggestionSet.Empty(input);
		}

		var extensions = NormalizeExtensions(fileExtensions);
		string[] entries;
		try
		{
			entries = Directory.EnumerateFileSystemEntries(search.Directory)
				.Where(path => Path.GetFileName(path).StartsWith(search.NamePrefix, StringComparison.OrdinalIgnoreCase))
				.ToArray();
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
		{
			return PathSuggestionSet.Empty(input);
		}

		var matches = entries
			.Select(path => CreateSuggestion(path, inputPrefix, search.TypedDirectory, search.DirectorySeparator))
			.Where(suggestion => suggestion.IsDirectory || mode == PathSelectionMode.File && MatchesExtension(suggestion.Value, extensions))
			.OrderByDescending(suggestion => suggestion.IsDirectory)
			.ThenBy(suggestion => suggestion.DisplayValue, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (matches.Length == 0)
		{
			return PathSuggestionSet.Empty(input);
		}

		var completionValue = matches.Length == 1
			? matches[0].Value
			: inputPrefix + LongestCommonPrefix(matches.Select(suggestion => suggestion.DisplayValue));
		if (completionValue.Length < input.Length)
		{
			completionValue = input;
		}

		return new PathSuggestionSet(matches.Take(MaximumSuggestions).ToArray(), completionValue);
	}

	private static (string Prefix, string Segment) SplitInput(string input, bool allowMultiple)
	{
		if (!allowMultiple)
		{
			return (string.Empty, input);
		}

		var separatorIndex = input.LastIndexOf(';');
		if (separatorIndex < 0)
		{
			return (string.Empty, input);
		}

		var segmentStart = separatorIndex + 1;
		while (segmentStart < input.Length && char.IsWhiteSpace(input[segmentStart]))
		{
			segmentStart++;
		}

		return (input[..segmentStart], input[segmentStart..]);
	}

	private static bool TryResolveSearch(string segment, string workingDirectory, out PathSearch search)
	{
		try
		{
			var expandedSegment = Environment.ExpandEnvironmentVariables(segment);
			var separator = segment.Contains('/') && !segment.Contains('\\') ? '/' : Path.DirectorySeparatorChar;
			string typedDirectory;
			string namePrefix;
			if (EndsWithDirectorySeparator(expandedSegment))
			{
				typedDirectory = segment;
				namePrefix = string.Empty;
			}
			else
			{
				namePrefix = Path.GetFileName(expandedSegment);
				typedDirectory = segment[..Math.Max(0, segment.Length - namePrefix.Length)];
			}

			var expandedTypedDirectory = Environment.ExpandEnvironmentVariables(typedDirectory);
			var directory = expandedTypedDirectory.Length == 0
				? Path.GetFullPath(workingDirectory)
				: Path.GetFullPath(expandedTypedDirectory, workingDirectory);
			search = new PathSearch(directory, typedDirectory, namePrefix, separator);
			return Directory.Exists(directory);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
		{
			search = default;
			return false;
		}
	}

	private static PathSuggestion CreateSuggestion(string path, string inputPrefix, string typedDirectory, char directorySeparator)
	{
		var isDirectory = Directory.Exists(path);
		var displayValue = typedDirectory + Path.GetFileName(path);
		if (isDirectory)
		{
			displayValue += directorySeparator;
		}

		return new PathSuggestion(inputPrefix + displayValue, displayValue, isDirectory);
	}

	private static HashSet<string> NormalizeExtensions(IReadOnlyCollection<string>? extensions)
	{
		if (extensions is null || extensions.Count == 0)
		{
			return [];
		}

		return extensions
			.Select(extension => extension.StartsWith('.') ? extension : "." + extension)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	private static bool MatchesExtension(string path, HashSet<string> extensions)
	{
		var result = extensions.Count == 0 || extensions.Contains(Path.GetExtension(path));

		return result;
	}

	private static bool EndsWithDirectorySeparator(string value)
	{
		var result = value.EndsWith(Path.DirectorySeparatorChar) || value.EndsWith(Path.AltDirectorySeparatorChar);

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

		return prefix;
	}

	private readonly record struct PathSearch(string Directory, string TypedDirectory, string NamePrefix, char DirectorySeparator);
}
