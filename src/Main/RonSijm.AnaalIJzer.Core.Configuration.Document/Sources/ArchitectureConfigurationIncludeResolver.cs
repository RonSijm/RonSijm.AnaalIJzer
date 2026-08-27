using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

public static class ArchitectureConfigurationIncludeResolver
{
	public static ImmutableArray<AdditionalText> ResolveAdditionalFiles(
		ImmutableArray<AdditionalText> additionalFiles,
		IReadOnlyDictionary<string, AdditionalText> additionalFileLookup,
		string configPath,
		string includePath,
		bool allowFileNameFallback)
	{
		if (string.IsNullOrWhiteSpace(includePath))
		{
			return [];
		}

		if (!HasWildcardPattern(includePath))
		{
			var resolvedPath = ArchitectureConfigurationSourceLookup.ResolveRelativePath(includePath, configPath);
			if (!ArchitectureConfigurationSourceLookup.TryFindIncludedFile(additionalFileLookup, resolvedPath, includePath, allowFileNameFallback, out var includeFile))
			{
				return [];
			}

			return [includeFile];
		}

		var matches = ImmutableArray.CreateBuilder<AdditionalText>();
		var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var normalizedConfigPath = ArchitectureConfigurationSourceLookup.NormalizePath(configPath);
		foreach (var file in additionalFiles.OrderBy(file => ArchitectureConfigurationSourceLookup.NormalizePath(file.Path), StringComparer.OrdinalIgnoreCase))
		{
			if (!MatchesIncludePath(configPath, includePath, file.Path))
			{
				continue;
			}

			var normalizedPath = ArchitectureConfigurationSourceLookup.NormalizePath(file.Path);
			if (string.Equals(normalizedPath, normalizedConfigPath, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!seenPaths.Add(normalizedPath))
			{
				continue;
			}

			matches.Add(file);
		}

		var result = matches.ToImmutable();

		return result;
	}

	public static ImmutableArray<string> ResolveFileSystemPaths(string configPath, string includePath)
	{
		if (string.IsNullOrWhiteSpace(includePath))
		{
			return [];
		}

		if (!HasWildcardPattern(includePath))
		{
			var resolvedPath = Path.GetFullPath(ArchitectureConfigurationSourceLookup.ResolveRelativePath(includePath, configPath));
			if (!File.Exists(resolvedPath))
			{
				return [];
			}

			return [resolvedPath];
		}

		var searchRoot = DetermineSearchRoot(configPath, includePath);
		if (string.IsNullOrWhiteSpace(searchRoot) || !Directory.Exists(searchRoot))
		{
			return [];
		}

		try
		{
			var matches = Directory
				.EnumerateFiles(searchRoot, "*", SearchOption.AllDirectories)
				.Where(path => MatchesIncludePath(configPath, includePath, path))
				.Select(Path.GetFullPath)
				.Where(path => !string.Equals(
					ArchitectureConfigurationSourceLookup.NormalizePath(path),
					ArchitectureConfigurationSourceLookup.NormalizePath(configPath),
					StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(path => ArchitectureConfigurationSourceLookup.NormalizePath(path), StringComparer.OrdinalIgnoreCase)
				.ToImmutableArray();

			return matches;
		}
		catch (IOException)
		{
			return [];
		}
		catch (UnauthorizedAccessException)
		{
			return [];
		}
	}

	public static string CreateMissingIncludeMessage(string includePath)
	{
		var result = HasWildcardPattern(includePath)
			? $"Included architecture configuration wildcard matched no files: {includePath}."
			: $"Included architecture configuration was not provided as an AdditionalFile: {includePath}.";

		return result;
	}

	public static bool HasWildcardPattern(string path)
	{
		var result = path.IndexOfAny(['*', '?']) >= 0;

		return result;
	}

	private static bool MatchesIncludePath(string configPath, string includePath, string candidatePath)
	{
		if (IsFileNameOnlyPattern(includePath))
		{
			var candidateFileName = ArchitectureConfigurationSourceLookup.GetFileNamePreservingStyle(candidatePath);
			var isMatch = GlobMatches(includePath, candidateFileName);

			return isMatch;
		}

		var resolvedPattern = ArchitectureConfigurationSourceLookup.NormalizePath(ArchitectureConfigurationSourceLookup.ResolveRelativePath(includePath, configPath));
		var normalizedCandidate = ArchitectureConfigurationSourceLookup.NormalizePath(candidatePath);
		var result = GlobMatches(resolvedPattern, normalizedCandidate);

		return result;
	}

	private static string DetermineSearchRoot(string configPath, string includePath)
	{
		var configurationDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));
		if (string.IsNullOrWhiteSpace(configurationDirectory))
		{
			return string.Empty;
		}

		if (IsFileNameOnlyPattern(includePath))
		{
			return configurationDirectory!;
		}

		var resolvedPattern = ArchitectureConfigurationSourceLookup.NormalizePath(ArchitectureConfigurationSourceLookup.ResolveRelativePath(includePath, configPath));
		var fixedPrefix = GetFixedPrefix(resolvedPattern);
		var candidateDirectory = ArchitectureConfigurationSourceLookup.GetDirectoryNamePreservingStyle(fixedPrefix);
		if (string.IsNullOrWhiteSpace(candidateDirectory))
		{
			return configurationDirectory!;
		}

		var currentDirectory = NormalizeForCurrentPlatform(candidateDirectory!);
		while (!string.IsNullOrWhiteSpace(currentDirectory) && !Directory.Exists(currentDirectory))
		{
			var parent = Directory.GetParent(currentDirectory);
			if (parent is null)
			{
				break;
			}

			currentDirectory = parent.FullName;
		}

		var result = Directory.Exists(currentDirectory) ? currentDirectory : configurationDirectory!;

		return result;
	}

	private static string GetFixedPrefix(string pattern)
	{
		var wildcardIndex = pattern.IndexOfAny(['*', '?']);
		var result = wildcardIndex < 0 ? pattern : pattern.Substring(0, wildcardIndex);

		return result;
	}

	private static bool GlobMatches(string pattern, string candidate)
	{
		var normalizedPattern = NormalizeGlobText(pattern);
		var normalizedCandidate = NormalizeGlobText(candidate);
		var regexPattern = "^"
		                   + Regex.Escape(normalizedPattern)
			                   .Replace(@"\*\*", "__DOUBLE_STAR__")
			                   .Replace(@"\*", "[^/]*")
			                   .Replace(@"\?", "[^/]")
			                   .Replace("__DOUBLE_STAR__", ".*")
		                   + "$";
		var result = Regex.IsMatch(normalizedCandidate, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		return result;
	}

	private static bool IsFileNameOnlyPattern(string includePath)
	{
		var result = !ArchitectureConfigurationSourceLookup.IsPathRootedPreservingStyle(includePath)
		             && includePath.IndexOfAny(['\\', '/']) < 0;

		return result;
	}

	private static string NormalizeForCurrentPlatform(string path)
	{
		var result = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

		return result;
	}

	private static string NormalizeGlobText(string value)
	{
		var result = value.Replace('\\', '/');

		return result;
	}
}
