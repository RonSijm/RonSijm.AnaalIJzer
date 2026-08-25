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

internal static partial class PathSuggestionProvider
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
}
