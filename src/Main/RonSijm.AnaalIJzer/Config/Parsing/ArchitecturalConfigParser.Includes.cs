using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Parsing;

internal static partial class ArchitecturalConfigParser
{
	private static string ResolveRelativePath(string path, string configFilePath)
	{
		if (Path.IsPathRooted(path))
		{
			return path;
		}

		var configDir = Path.GetDirectoryName(configFilePath);
		return configDir is null ? path : Path.Combine(configDir, path);
	}

	private static Dictionary<string, AdditionalText> BuildAdditionalFileLookup(ImmutableArray<AdditionalText> additionalFiles)
	{
		var lookup = new Dictionary<string, AdditionalText>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in additionalFiles)
		{
			lookup[NormalizePath(file.Path)] = file;

			var fileName = Path.GetFileName(file.Path);
			if (!string.IsNullOrEmpty(fileName) && !lookup.ContainsKey(fileName))
			{
				lookup[fileName] = file;
			}
		}

		return lookup;
	}

	private static bool TryFindIncludedFile(IReadOnlyDictionary<string, AdditionalText> additionalFileLookup, string resolvedPath, string includePath, bool allowFileNameFallback, out AdditionalText includeFile)
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

		var fileName = Path.GetFileName(includePath);
		return !string.IsNullOrEmpty(fileName) && additionalFileLookup.TryGetValue(fileName, out includeFile!);
	}

	private static void CollectConfig(string content, string configPath, IReadOnlyDictionary<string, AdditionalText> additionalFileLookup, CancellationToken cancellationToken, List<(XElement Root, string Path)> documents, List<(XElement Element, string Path)> elements, ImmutableArray<ArchitectureDocumentationItem>.Builder documentationItems, ImmutableArray<ConfigurationIssue>.Builder issues, HashSet<string> activePaths, HashSet<string> visitedPaths)
	{
		var normalizedPath = NormalizePath(configPath);
		if (!activePaths.Add(normalizedPath))
		{
			return;
		}

		if (!visitedPaths.Add(normalizedPath))
		{
			activePaths.Remove(normalizedPath);
			return;
		}

		// SetLineInfo lets us recover the originating XML element location for each
		// rule, which the "Add to exceptions" code fix uses to find the matcher
		// element that owns the <Exceptions> block.
		var doc = XDocument.Parse(content, LoadOptions.SetLineInfo);
		ValidateDocument(doc, configPath, issues);
		if (doc.Root is null)
		{
			activePaths.Remove(normalizedPath);
			return;
		}

		documents.Add((doc.Root, configPath));

		foreach (var child in doc.Root.Elements())
		{
			AddDocumentationItems(child, configPath, 0, string.Empty, documentationItems);

			if (child.Name.LocalName != "Include")
			{
				elements.Add((child, configPath));
				continue;
			}

			if (child.Attribute("path")?.Value is not { } includePath || string.IsNullOrWhiteSpace(includePath))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Include requires a non-empty path.", child, configPath);
				continue;
			}

			var resolvedPath = ResolveRelativePath(includePath, configPath);
			var allowFileNameFallback = string.Equals(configPath, InlineSettingsMetadataKey, StringComparison.Ordinal)
			                            || string.IsNullOrEmpty(Path.GetDirectoryName(configPath));
			if (!TryFindIncludedFile(additionalFileLookup, resolvedPath, includePath, allowFileNameFallback, out var includeFile))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Included architecture configuration was not provided as an AdditionalFile: {includePath}.", child, configPath);
				continue;
			}

			var includeText = includeFile.GetText(cancellationToken);
			var includeContent = includeText?.ToString();
			if (string.IsNullOrWhiteSpace(includeContent))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"Included architecture configuration is empty: {includePath}.", child, configPath);
				continue;
			}

			CollectConfig(includeContent!, includeFile.Path, additionalFileLookup, cancellationToken, documents, elements, documentationItems, issues, activePaths, visitedPaths);
		}

		activePaths.Remove(normalizedPath);
	}
}
