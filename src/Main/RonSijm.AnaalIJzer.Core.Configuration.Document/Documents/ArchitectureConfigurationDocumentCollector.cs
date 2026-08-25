using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Document;

public static class ArchitectureConfigurationDocumentCollector
{
	public static ArchitectureConfigurationCollectionResult Collect(
		string content,
		string configPath,
		IReadOnlyDictionary<string, AdditionalText> additionalFileLookup,
		CancellationToken cancellationToken,
		Func<XDocument, string, ImmutableArray<ConfigurationIssue>> validateDocument,
		string inlineSettingsMetadataKey,
		bool isInlineConfiguration)
	{
		var documents = ImmutableArray.CreateBuilder<ArchitectureConfigurationCollectedDocument>();
		var elements = ImmutableArray.CreateBuilder<ArchitectureConfigurationCollectedElement>();
		var documentationItems = ImmutableArray.CreateBuilder<ArchitectureDocumentationItem>();
		var issues = ImmutableArray.CreateBuilder<ConfigurationIssue>();

		CollectCore(
			content,
			configPath,
			additionalFileLookup,
			cancellationToken,
			validateDocument,
			inlineSettingsMetadataKey,
			isInlineConfiguration,
			documents,
			elements,
			documentationItems,
			issues,
			new HashSet<string>(StringComparer.OrdinalIgnoreCase),
			new HashSet<string>(StringComparer.OrdinalIgnoreCase));

		var result = new ArchitectureConfigurationCollectionResult(
			documents.ToImmutable(),
			elements.ToImmutable(),
			documentationItems.ToImmutable(),
			issues.ToImmutable());

		return result;
	}

	private static void CollectCore(
		string content,
		string configPath,
		IReadOnlyDictionary<string, AdditionalText> additionalFileLookup,
		CancellationToken cancellationToken,
		Func<XDocument, string, ImmutableArray<ConfigurationIssue>> validateDocument,
		string inlineSettingsMetadataKey,
		bool isInlineConfiguration,
		ImmutableArray<ArchitectureConfigurationCollectedDocument>.Builder documents,
		ImmutableArray<ArchitectureConfigurationCollectedElement>.Builder elements,
		ImmutableArray<ArchitectureDocumentationItem>.Builder documentationItems,
		ImmutableArray<ConfigurationIssue>.Builder issues,
		HashSet<string> activePaths,
		HashSet<string> visitedPaths)
	{
		var normalizedPath = ArchitectureConfigurationSourceLookup.NormalizePath(configPath);
		if (!activePaths.Add(normalizedPath))
		{
			return;
		}

		if (!visitedPaths.Add(normalizedPath))
		{
			activePaths.Remove(normalizedPath);

			return;
		}

		var document = XDocument.Parse(content, LoadOptions.SetLineInfo);
		issues.AddRange(validateDocument(document, configPath));
		if (document.Root is null)
		{
			activePaths.Remove(normalizedPath);

			return;
		}

		documents.Add(new ArchitectureConfigurationCollectedDocument(document.Root, configPath, isInlineConfiguration));

		foreach (var child in document.Root.Elements())
		{
			ArchitectureConfigurationDocumentationBuilder.AddDocumentationItems(child, configPath, 0, string.Empty, documentationItems);

			if (child.Name.LocalName != "Include")
			{
				elements.Add(new ArchitectureConfigurationCollectedElement(child, configPath, isInlineConfiguration));

				continue;
			}

			if (child.Attribute("path")?.Value is not { } includePath || string.IsNullOrWhiteSpace(includePath))
			{
				AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, "Include requires a non-empty path.", child, configPath);

				continue;
			}

			var resolvedPath = ArchitectureConfigurationSourceLookup.ResolveRelativePath(includePath, configPath);
			var allowFileNameFallback = isInlineConfiguration || string.Equals(configPath, inlineSettingsMetadataKey, StringComparison.Ordinal)
			                            || string.IsNullOrEmpty(Path.GetDirectoryName(configPath));
			if (!ArchitectureConfigurationSourceLookup.TryFindIncludedFile(additionalFileLookup, resolvedPath, includePath, allowFileNameFallback, out var includeFile))
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

			CollectCore(
				includeContent!,
				includeFile.Path,
				additionalFileLookup,
				cancellationToken,
				validateDocument,
				inlineSettingsMetadataKey,
				false,
				documents,
				elements,
				documentationItems,
				issues,
				activePaths,
				visitedPaths);
		}

		activePaths.Remove(normalizedPath);
	}

	private static void AddIssue(ImmutableArray<ConfigurationIssue>.Builder issues, ConfigurationIssueKind kind, string message, XElement element, string path)
	{
		var line = (IXmlLineInfo)element;
		issues.Add(new ConfigurationIssue(kind, message, path, line.HasLineInfo() ? line.LineNumber : 0, line.HasLineInfo() ? line.LinePosition : 0));
	}
}
