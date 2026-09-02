using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class ConfigurationCodeFixEditor
{
	internal static async Task<Solution> EditConfigurationAsync(Document triggeringDocument, ArchitectureConfigurationSource source, Func<XDocument, bool> edit, CancellationToken cancellationToken)
	{
		if (!source.CanEdit)
		{
			return triggeringDocument.Project.Solution;
		}

		if (source.Kind == ArchitectureConfigurationSourceKind.XmlFile)
		{
			var result = await EditXmlConfigurationAsync(triggeringDocument.Project, source.Path, edit, cancellationToken);

			return result;
		}

		if (source.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata)
		{
			var result = await EditInlineConfigurationAsync(triggeringDocument.Project, source.Path, edit, cancellationToken);

			return result;
		}

		return triggeringDocument.Project.Solution;
	}

	private static async Task<Solution> EditXmlConfigurationAsync(Project project, string path, Func<XDocument, bool> edit, CancellationToken cancellationToken)
	{
		var configDocument = FindAdditionalDocument(project, path);
		if (configDocument is null)
		{
			return project.Solution;
		}

		var originalText = await configDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
		var document = XDocument.Parse(originalText.ToString(), LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
		var changed = edit(document);
		if (!changed)
		{
			return project.Solution;
		}

		var updatedXml = ArchitectureConfigurationXmlSerializer.SerializeXml(document);
		var updatedText = SourceText.From(updatedXml, originalText.Encoding ?? Encoding.UTF8);
		var result = project.Solution.WithAdditionalDocumentText(configDocument.Id, updatedText);

		return result;
	}

	private static async Task<Solution> EditInlineConfigurationAsync(Project project, string path, Func<XDocument, bool> edit, CancellationToken cancellationToken)
	{
		var sourceDocument = FindSourceDocument(project, path);
		if (sourceDocument is null)
		{
			return project.Solution;
		}

		var originalText = await sourceDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
		var source = originalText.ToString();
		if (!InlineAssemblyMetadataSettings.TryFindInlineSettings(source, out var settings, out _))
		{
			return project.Solution;
		}

		var document = XDocument.Parse(settings.Xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
		var changed = edit(document);
		if (!changed)
		{
			return project.Solution;
		}

		var updatedXml = ArchitectureConfigurationXmlSerializer.SerializeXml(document);
		if (!InlineAssemblyMetadataSettings.TryCreateInlineSettingsLiteral(settings, updatedXml, InlineAssemblyMetadataSettings.DetectNewLine(source), out var updatedLiteral, out _))
		{
			return project.Solution;
		}

		var updatedSource = source.Remove(settings.LiteralSpan.Start, settings.LiteralSpan.Length)
			.Insert(settings.LiteralSpan.Start, updatedLiteral);
		var updatedText = SourceText.From(updatedSource, originalText.Encoding ?? Encoding.UTF8);
		var result = project.Solution.WithDocumentText(sourceDocument.Id, updatedText);

		return result;
	}

	private static TextDocument? FindAdditionalDocument(Project project, string path)
	{
		TextDocument? fileNameMatch = null;
		var normalizedPath = ArchitectureConfigurationSourceLookup.NormalizePath(path);
		var expectedFileName = ArchitectureConfigurationSourceLookup.GetFileNamePreservingStyle(path);
		foreach (var document in project.AdditionalDocuments)
		{
			if (document.FilePath is not { } documentPath)
			{
				continue;
			}

			if (string.Equals(ArchitectureConfigurationSourceLookup.NormalizePath(documentPath), normalizedPath, StringComparison.OrdinalIgnoreCase))
			{
				return document;
			}

			if (string.Equals(ArchitectureConfigurationSourceLookup.GetFileNamePreservingStyle(documentPath), expectedFileName, StringComparison.OrdinalIgnoreCase))
			{
				fileNameMatch ??= document;
			}
		}

		return fileNameMatch;
	}

	private static Document? FindSourceDocument(Project project, string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			foreach (var document in project.Documents)
			{
				var source = document.GetTextAsync(CancellationToken.None).GetAwaiter().GetResult().ToString();
				if (InlineAssemblyMetadataSettings.TryFindInlineSettings(source, out _, out _))
				{
					return document;
				}
			}

			return null;
		}

		Document? fileNameMatch = null;
		var normalizedPath = ArchitectureConfigurationSourceLookup.NormalizePath(path);
		var expectedFileName = ArchitectureConfigurationSourceLookup.GetFileNamePreservingStyle(path);
		foreach (var document in project.Documents)
		{
			if (document.FilePath is not { } documentPath)
			{
				continue;
			}

			if (string.Equals(ArchitectureConfigurationSourceLookup.NormalizePath(documentPath), normalizedPath, StringComparison.OrdinalIgnoreCase))
			{
				return document;
			}

			if (string.Equals(ArchitectureConfigurationSourceLookup.GetFileNamePreservingStyle(documentPath), expectedFileName, StringComparison.OrdinalIgnoreCase))
			{
				fileNameMatch ??= document;
			}
		}

		if (fileNameMatch is not null)
		{
			return fileNameMatch;
		}

		foreach (var document in project.Documents)
		{
			var source = document.GetTextAsync(CancellationToken.None).GetAwaiter().GetResult().ToString();
			if (InlineAssemblyMetadataSettings.TryFindInlineSettings(source, out _, out _))
			{
				return document;
			}
		}

		return fileNameMatch;
	}
}
