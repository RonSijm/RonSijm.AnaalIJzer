using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static class ArchitectureConfigurationSourceLookup
{
	public static string ResolveRelativePath(string path, string configFilePath)
	{
		if (IsPathRootedPreservingStyle(path))
		{
			return path;
		}

		var configDir = GetDirectoryNamePreservingStyle(configFilePath);
		var result = configDir is null ? path : CombinePathPreservingStyle(configDir, path);

		return result;
	}

	public static Dictionary<string, AdditionalText> BuildAdditionalFileLookup(ImmutableArray<AdditionalText> additionalFiles)
	{
		var lookup = new Dictionary<string, AdditionalText>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in additionalFiles)
		{
			lookup[NormalizePath(file.Path)] = file;

			var fileName = GetFileNamePreservingStyle(file.Path);
			if (!string.IsNullOrEmpty(fileName) && !lookup.ContainsKey(fileName))
			{
				lookup[fileName] = file;
			}
		}

		return lookup;
	}

	public static bool TryFindIncludedFile(IReadOnlyDictionary<string, AdditionalText> additionalFileLookup, string resolvedPath, string includePath, bool allowFileNameFallback, out AdditionalText includeFile)
	{
		if (additionalFileLookup.TryGetValue(NormalizePath(resolvedPath), out includeFile!))
		{
			return true;
		}

		if (!allowFileNameFallback)
		{
			return false;
		}

		if (additionalFileLookup.TryGetValue(includePath, out includeFile!))
		{
			return true;
		}

		var fileName = GetFileNamePreservingStyle(includePath);
		var result = !string.IsNullOrEmpty(fileName) && additionalFileLookup.TryGetValue(fileName, out includeFile!);

		return result;
	}

	public static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		var normalizedSeparators = NormalizeSeparators(path);
		if (TryNormalizeAbsolutePath(normalizedSeparators, out var normalizedAbsolutePath))
		{
			var result = CollapseAbsolutePathSegments(normalizedAbsolutePath);

			return result;
		}

		try
		{
			var result = NormalizeSeparators(Path.GetFullPath(normalizedSeparators));

			return result;
		}
		catch
		{
			return normalizedSeparators;
		}
	}

	public static string GetFileNamePreservingStyle(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		var trimmedPath = TrimEndingSeparators(path);
		var separatorIndex = trimmedPath.LastIndexOfAny(['\\', '/']);
		var result = separatorIndex < 0 ? trimmedPath : trimmedPath.Substring(separatorIndex + 1);

		return result;
	}

	public static string? GetDirectoryNamePreservingStyle(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		var trimmedPath = TrimEndingSeparators(path);
		var separatorIndex = trimmedPath.LastIndexOfAny(['\\', '/']);
		if (separatorIndex < 0)
		{
			return null;
		}

		if (separatorIndex == 0)
		{
			return trimmedPath.Substring(0, 1);
		}

		if (separatorIndex == 2 && IsWindowsDrivePath(trimmedPath))
		{
			return trimmedPath.Substring(0, 3);
		}

		var result = trimmedPath.Substring(0, separatorIndex);

		return result;
	}

	public static bool IsPathRootedPreservingStyle(string path)
	{
		if (Path.IsPathRooted(path))
		{
			return true;
		}

		var normalizedPath = NormalizeSeparators(path);
		var result = TryNormalizeAbsolutePath(normalizedPath, out _);

		return result;
	}

	public static string CombinePathPreservingStyle(string directory, string fileName)
	{
		var trimmedDirectory = TrimEndingSeparators(directory);
		var separator = GetPreferredSeparator(directory);
		var result = trimmedDirectory + separator + fileName;

		return result;
	}

	private static bool TryNormalizeAbsolutePath(string path, out string normalizedAbsolutePath)
	{
		if (IsWindowsDrivePath(path))
		{
			var driveLetter = char.ToUpperInvariant(path[0]);
			var remainder = path.Substring(3).TrimStart('/');
			normalizedAbsolutePath = remainder.Length == 0 ? driveLetter + ":/" : driveLetter + ":/" + remainder;

			return true;
		}

		if (path.StartsWith("//", StringComparison.Ordinal))
		{
			var trimmed = path.TrimEnd('/');
			normalizedAbsolutePath = trimmed.Length == 0 ? "//" : trimmed;

			return true;
		}

		if (path.StartsWith("/", StringComparison.Ordinal))
		{
			var trimmed = path.TrimEnd('/');
			normalizedAbsolutePath = trimmed.Length == 0 ? "/" : trimmed;

			return true;
		}

		normalizedAbsolutePath = string.Empty;

		return false;
	}

	private static string CollapseAbsolutePathSegments(string path)
	{
		var prefixLength = GetAbsolutePathPrefixLength(path);
		if (prefixLength <= 0 || prefixLength >= path.Length)
		{
			return path;
		}

		var prefix = path.Substring(0, prefixLength);
		var remainder = path.Substring(prefixLength);
		var segments = remainder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		var collapsedSegments = new List<string>(segments.Length);
		foreach (var segment in segments)
		{
			if (segment == ".")
			{
				continue;
			}

			if (segment == "..")
			{
				if (collapsedSegments.Count > 0)
				{
					collapsedSegments.RemoveAt(collapsedSegments.Count - 1);
				}

				continue;
			}

			collapsedSegments.Add(segment);
		}

		var collapsedRemainder = string.Join("/", collapsedSegments);
		var result = collapsedRemainder.Length == 0
			? prefix.TrimEnd('/')
			: prefix + collapsedRemainder;

		return result;
	}

	private static int GetAbsolutePathPrefixLength(string path)
	{
		if (IsWindowsDrivePath(path))
		{
			return 3;
		}

		if (path.StartsWith("//", StringComparison.Ordinal))
		{
			return 2;
		}

		if (path.StartsWith("/", StringComparison.Ordinal))
		{
			return 1;
		}

		return 0;
	}

	private static string NormalizeSeparators(string path)
	{
		var result = path.Replace('\\', '/');

		return result;
	}

	private static string TrimEndingSeparators(string path)
	{
		if (path.Length <= 1)
		{
			return path;
		}

		if (path.Length == 3 && IsWindowsDrivePath(path))
		{
			return path;
		}

		var result = path.TrimEnd('\\', '/');

		return string.IsNullOrEmpty(result) ? path : result;
	}

	private static char GetPreferredSeparator(string path)
	{
		if (path.Contains('\\') && !path.Contains('/'))
		{
			return '\\';
		}

		if (path.Contains('/') && !path.Contains('\\'))
		{
			return '/';
		}

		return Path.DirectorySeparatorChar;
	}

	private static bool IsWindowsDrivePath(string path)
	{
		var result = path.Length >= 3
		             && char.IsLetter(path[0])
		             && path[1] == ':'
		             && (path[2] == '\\' || path[2] == '/');

		return result;
	}
}
