using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class NameRuleAllowMappingCodeFix
{
	private const string RequireMatchingNamesRuleKind = "RequireMatchingNames";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!TryReadFixData(diagnostic, out var sourceName, out var targetName, out var site, out var xmlLineNumber, out var xmlLinePosition, out var sourcePath))
		{
			return;
		}

		var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
		if (compilation is null)
		{
			return;
		}

		var discoveredSource = ArchitectureConfigurationSourceDiscovery.FindConfigurationSource(
			context.Document.FilePath,
			context.Document.Project.AnalyzerOptions.AdditionalFiles,
			compilation,
			context.CancellationToken);
		var configurationSource = ResolveSource(discoveredSource, sourcePath);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		var broadTitle = $"Add <Allow from=\"{sourceName}\" to=\"{targetName}\" /> to name rule";
		context.RegisterCodeFix(
			CodeAction.Create(
				broadTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowMapping(document, xmlLineNumber, xmlLinePosition, sourceName, targetName, null),
					cancellationToken),
				broadTitle),
			diagnostic);

		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var scopedTitle = $"Add site-scoped <Allow from=\"{sourceName}\" to=\"{targetName}\" /> for {site}";
		context.RegisterCodeFix(
			CodeAction.Create(
				scopedTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowMapping(document, xmlLineNumber, xmlLinePosition, sourceName, targetName, site),
					cancellationToken),
				scopedTitle),
			diagnostic);
	}

	private static bool TryAddAllowMapping(XDocument document, int xmlLineNumber, int xmlLinePosition, string sourceName, string targetName, string? site)
	{
		var ruleElement = FindNameRuleElement(document, xmlLineNumber, xmlLinePosition);
		if (ruleElement is null)
		{
			return false;
		}

		var existingAllow = ruleElement
			.Elements("Allow")
			.FirstOrDefault(element =>
				string.Equals(element.Attribute("from")?.Value, sourceName, StringComparison.Ordinal)
				&& string.Equals(element.Attribute("to")?.Value, targetName, StringComparison.Ordinal));
		if (existingAllow is not null)
		{
			return TryUpdateExistingAllow(existingAllow, site);
		}

		var element = new XElement(
			"Allow",
			new XAttribute("from", sourceName),
			new XAttribute("to", targetName));
		if (!string.IsNullOrWhiteSpace(site))
		{
			element.SetAttributeValue("allowedSites", site);
		}

		ruleElement.Add(element);
		return true;
	}

	private static bool TryUpdateExistingAllow(XElement allowElement, string? site)
	{
		if (string.IsNullOrWhiteSpace(site))
		{
			var hadAllowedSites = allowElement.Attribute("allowedSites") is not null;
			allowElement.SetAttributeValue("allowedSites", null);

			return hadAllowedSites;
		}

		var requiredSite = site!;
		var allowedSites = ReadSites(allowElement.Attribute("allowedSites")?.Value);
		if (allowedSites.Contains(requiredSite))
		{
			return false;
		}

		allowedSites.Add(requiredSite);
		allowElement.SetAttributeValue("allowedSites", string.Join(", ", allowedSites));
		return true;
	}

	private static SortedSet<string> ReadSites(string? attributeValue)
	{
		var result = new SortedSet<string>(StringComparer.Ordinal);
		if (string.IsNullOrWhiteSpace(attributeValue))
		{
			return result;
		}

		var value = attributeValue!;
		foreach (var token in value.Split(','))
		{
			var trimmed = token.Trim();
			if (trimmed.Length > 0)
			{
				result.Add(trimmed);
			}
		}

		return result;
	}

	private static XElement? FindNameRuleElement(XDocument document, int xmlLineNumber, int xmlLinePosition)
	{
		var elements = document.Descendants(RequireMatchingNamesRuleKind).ToArray();
		if (xmlLineNumber > 0)
		{
			var byLine = elements.FirstOrDefault(element =>
			{
				var lineInfo = (IXmlLineInfo)element;
				var result = lineInfo.HasLineInfo()
				             && lineInfo.LineNumber == xmlLineNumber
				             && (xmlLinePosition <= 0 || lineInfo.LinePosition == xmlLinePosition);

				return result;
			});
			if (byLine is not null)
			{
				return byLine;
			}
		}

		return null;
	}

	private static bool TryReadFixData(Diagnostic diagnostic, out string sourceName, out string targetName, out string site, out int xmlLineNumber, out int xmlLinePosition, out string sourcePath)
	{
		sourceName = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyNormalizedSourceName, out var normalizedSourceValue)
			? normalizedSourceValue ?? string.Empty
			: string.Empty;
		targetName = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyNormalizedTargetName, out var normalizedTargetValue)
			? normalizedTargetValue ?? string.Empty
			: string.Empty;
		site = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertySite, out var siteValue)
			? siteValue ?? string.Empty
			: string.Empty;
		sourcePath = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyRuleXmlPath, out var sourcePathValue)
			? sourcePathValue ?? string.Empty
			: string.Empty;
		xmlLineNumber = TryReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		xmlLinePosition = TryReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);

		var result = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyNameRuleKind, out var ruleKind)
		             && string.Equals(ruleKind, RequireMatchingNamesRuleKind, StringComparison.Ordinal)
		             && !string.IsNullOrWhiteSpace(sourceName)
		             && !string.IsNullOrWhiteSpace(targetName)
		             && !string.Equals(sourceName, targetName, StringComparison.Ordinal)
		             && !string.IsNullOrWhiteSpace(sourcePath)
		             && xmlLineNumber > 0;

		return result;
	}

	private static int TryReadIntProperty(Diagnostic diagnostic, string propertyName)
	{
		if (!diagnostic.Properties.TryGetValue(propertyName, out var value)
		    || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
		{
			return 0;
		}

		return parsed;
	}

	private static ArchitectureConfigurationSource ResolveSource(ArchitectureConfigurationSource discoveredSource, string sourcePath)
	{
		if (discoveredSource.Kind == ArchitectureConfigurationSourceKind.InlineAssemblyMetadata
		    && string.Equals(
			    ArchitectureConfigurationSourceLookup.NormalizePath(discoveredSource.Path),
			    ArchitectureConfigurationSourceLookup.NormalizePath(sourcePath),
			    StringComparison.OrdinalIgnoreCase))
		{
			return discoveredSource;
		}

		var result = new ArchitectureConfigurationSource(ArchitectureConfigurationSourceKind.XmlFile, sourcePath);

		return result;
	}
}
