using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class BoundaryEntryPointCodeFix
{
	private const string SiteFilterFailurePrefix = "the matching entry point does not allow site ";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var dependencyLayerName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDepLayerName);
		var boundaryLayerName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyBoundaryLayerName);
		var violationReason = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyViolationReason);
		var site = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertySite);
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlPath);
		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);
		if (string.IsNullOrWhiteSpace(dependencyLayerName)
		    || string.IsNullOrWhiteSpace(boundaryLayerName)
		    || string.IsNullOrWhiteSpace(sourcePath)
		    || xmlLineNumber <= 0)
		{
			return;
		}

		var discoveredSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var configurationSource = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, sourcePath, snapshots);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		var snapshot = snapshots.FirstOrDefault(candidate =>
			string.Equals(candidate.Source.Path, configurationSource.Path, StringComparison.OrdinalIgnoreCase));
		if (snapshot is null)
		{
			return;
		}

		var entryPointElement = ConfigurationCodeFixSupport.FindElementByLineInfo(snapshot.Document, "EntryPoint", xmlLineNumber, xmlLinePosition);
		if (entryPointElement is not null && violationReason.StartsWith(SiteFilterFailurePrefix, StringComparison.Ordinal))
		{
			RegisterEntryPointSiteFix(context, diagnostic, configurationSource, entryPointElement, site, xmlLineNumber, xmlLinePosition, dependencyLayerName);
			return;
		}

		RegisterAddEntryPointFixes(context, diagnostic, configurationSource, boundaryLayerName, dependencyLayerName, site, xmlLineNumber, xmlLinePosition);
	}

	private static void RegisterEntryPointSiteFix(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		XElement entryPointElement,
		string site,
		int xmlLineNumber,
		int xmlLinePosition,
		string dependencyLayerName)
	{
		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var allowedSites = ConfigurationCodeFixSupport.ReadSites(entryPointElement.Attribute("allowedSites")?.Value);
		if (allowedSites.Count > 0 && !allowedSites.Contains(site))
		{
			var title = $"Add site '{site}' to entry point for '{dependencyLayerName}'";
			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
						context.Document,
						configurationSource,
						document => TryAppendEntryPointAllowedSite(document, xmlLineNumber, xmlLinePosition, site),
						cancellationToken),
					title),
				diagnostic);
			return;
		}

		var blockedSites = ConfigurationCodeFixSupport.ReadSites(entryPointElement.Attribute("blockedSites")?.Value);
		if (!blockedSites.Contains(site))
		{
			return;
		}

		var title2 = $"Remove site '{site}' from blocked entry point for '{dependencyLayerName}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title2,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryRemoveEntryPointBlockedSite(document, xmlLineNumber, xmlLinePosition, site),
					cancellationToken),
				title2),
			diagnostic);
	}

	private static void RegisterAddEntryPointFixes(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		string boundaryLayerName,
		string dependencyLayerName,
		string site,
		int xmlLineNumber,
		int xmlLinePosition)
	{
		var layerReference = FormatEntryPointLayerReference(boundaryLayerName, dependencyLayerName);
		var broadTitle = $"Add entry point '{layerReference}' to boundary '{boundaryLayerName}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				broadTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddEntryPoint(document, xmlLineNumber, xmlLinePosition, layerReference, null),
					cancellationToken),
				broadTitle),
			diagnostic);

		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var scopedTitle = $"Add entry point '{layerReference}' to boundary '{boundaryLayerName}' at {site}";
		context.RegisterCodeFix(
			CodeAction.Create(
				scopedTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddEntryPoint(document, xmlLineNumber, xmlLinePosition, layerReference, site),
					cancellationToken),
				scopedTitle),
			diagnostic);
	}

	private static bool TryAppendEntryPointAllowedSite(XDocument document, int xmlLineNumber, int xmlLinePosition, string site)
	{
		var entryPointElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "EntryPoint", xmlLineNumber, xmlLinePosition);
		if (entryPointElement is null)
		{
			return false;
		}

		var allowedSites = ConfigurationCodeFixSupport.ReadSites(entryPointElement.Attribute("allowedSites")?.Value);
		if (!allowedSites.Add(site))
		{
			return false;
		}

		entryPointElement.SetAttributeValue("allowedSites", ConfigurationCodeFixSupport.FormatSites(allowedSites));
		return true;
	}

	private static bool TryRemoveEntryPointBlockedSite(XDocument document, int xmlLineNumber, int xmlLinePosition, string site)
	{
		var entryPointElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "EntryPoint", xmlLineNumber, xmlLinePosition);
		if (entryPointElement is null)
		{
			return false;
		}

		var blockedSites = ConfigurationCodeFixSupport.ReadSites(entryPointElement.Attribute("blockedSites")?.Value);
		if (!blockedSites.Remove(site))
		{
			return false;
		}

		entryPointElement.SetAttributeValue(
			"blockedSites",
			blockedSites.Count == 0 ? null : ConfigurationCodeFixSupport.FormatSites(blockedSites));
		return true;
	}

	private static bool TryAddEntryPoint(XDocument document, int xmlLineNumber, int xmlLinePosition, string layerReference, string? site)
	{
		var container = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "EntryPoints", xmlLineNumber, xmlLinePosition);
		if (container is null)
		{
			var entryPointElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "EntryPoint", xmlLineNumber, xmlLinePosition);
			container = entryPointElement?.Parent;
		}

		if (container is null)
		{
			return false;
		}

		var existing = container.Elements("EntryPoint")
			.FirstOrDefault(element => string.Equals(element.Attribute("layer")?.Value, layerReference, StringComparison.Ordinal));
		if (existing is not null)
		{
			if (string.IsNullOrWhiteSpace(site))
			{
				return false;
			}

			var allowedSites = ConfigurationCodeFixSupport.ReadSites(existing.Attribute("allowedSites")?.Value);
			if (!allowedSites.Add(site!))
			{
				return false;
			}

			existing.SetAttributeValue("allowedSites", ConfigurationCodeFixSupport.FormatSites(allowedSites));
			return true;
		}

		var element = new XElement("EntryPoint", new XAttribute("layer", layerReference));
		if (!string.IsNullOrWhiteSpace(site))
		{
			element.SetAttributeValue("allowedSites", site);
		}

		container.Add(element);
		return true;
	}

	private static string FormatEntryPointLayerReference(string boundaryLayerName, string dependencyLayerName)
	{
		if (dependencyLayerName.StartsWith(boundaryLayerName + "/", StringComparison.Ordinal))
		{
			var result = dependencyLayerName.Substring(boundaryLayerName.Length + 1);

			return result;
		}

		if (string.Equals(boundaryLayerName, dependencyLayerName, StringComparison.Ordinal))
		{
			return boundaryLayerName;
		}

		var rootQualifiedResult = "/" + dependencyLayerName;

		return rootQualifiedResult;
	}
}
