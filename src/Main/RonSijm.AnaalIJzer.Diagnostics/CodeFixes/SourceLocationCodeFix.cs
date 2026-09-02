using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Sources;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class SourceLocationCodeFix
{
	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!TryReadFixData(diagnostic, out var normalizedSourcePath, out var layerName, out var xmlLineNumber, out var xmlLinePosition, out var sourcePath))
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

		var title = $"Add source location '{normalizedSourcePath}' to layer '{layerName}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddSourceRule(document, xmlLineNumber, xmlLinePosition, normalizedSourcePath),
					cancellationToken),
				title),
			diagnostic);
	}

	private static bool TryAddSourceRule(XDocument document, int xmlLineNumber, int xmlLinePosition, string normalizedSourcePath)
	{
		var sourceLocations = FindSourceLocationsElement(document, xmlLineNumber, xmlLinePosition);
		if (sourceLocations is null)
		{
			return false;
		}

		var existingRule = sourceLocations
			.Elements("Source")
			.Any(element => string.Equals(element.Attribute("exactName")?.Value, normalizedSourcePath, StringComparison.Ordinal));
		if (existingRule)
		{
			return false;
		}

		sourceLocations.Add(new XElement("Source", new XAttribute("exactName", normalizedSourcePath)));
		return true;
	}

	private static XElement? FindSourceLocationsElement(XDocument document, int xmlLineNumber, int xmlLinePosition)
	{
		var elements = document.Descendants("SourceLocations").ToArray();
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

	private static bool TryReadFixData(Diagnostic diagnostic, out string normalizedSourcePath, out string layerName, out int xmlLineNumber, out int xmlLinePosition, out string sourcePath)
	{
		normalizedSourcePath = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyNormalizedSourcePath, out var normalizedPathValue)
			? normalizedPathValue ?? string.Empty
			: string.Empty;
		layerName = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyCallerLayerName, out var layerNameValue)
			? layerNameValue ?? string.Empty
			: string.Empty;
		sourcePath = diagnostic.Properties.TryGetValue(ArchitecturalDiagnostics.PropertyRuleXmlPath, out var sourcePathValue)
			? sourcePathValue ?? string.Empty
			: string.Empty;
		xmlLineNumber = TryReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		xmlLinePosition = TryReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);

		var result = !string.IsNullOrWhiteSpace(normalizedSourcePath)
		             && !string.IsNullOrWhiteSpace(layerName)
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
