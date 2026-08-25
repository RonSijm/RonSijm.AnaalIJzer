namespace RonSijm.AnaalIJzer.Arse.Components;

internal static partial class PathSuggestionProvider
{
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

		var result = new PathSuggestion(inputPrefix + displayValue, displayValue, isDirectory);

		return result;
	}

	private static bool EndsWithDirectorySeparator(string value)
	{
		var result = value.EndsWith(Path.DirectorySeparatorChar) || value.EndsWith(Path.AltDirectorySeparatorChar);

		return result;
	}

	private readonly record struct PathSearch(string Directory, string TypedDirectory, string NamePrefix, char DirectorySeparator);
}
