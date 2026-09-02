using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class ApiSurfacePolicyCodeFix
{
	private const string UnrecognizedLayerName = "unrecognized";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var dependencyLayerName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDepLayerName);
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlPath);
		var site = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertySite);
		var violationReason = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyViolationReason);
		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);
		if (string.IsNullOrWhiteSpace(sourcePath) || xmlLineNumber <= 0)
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

		var policyElement = ConfigurationCodeFixSupport.FindElementByLineInfo(snapshot.Document, "ApiSurface", xmlLineNumber, xmlLinePosition);
		if (policyElement is null)
		{
			return;
		}

		if (string.Equals(dependencyLayerName, UnrecognizedLayerName, StringComparison.Ordinal)
		    && violationReason.Contains("requires exposed types to belong to a configured layer", StringComparison.Ordinal))
		{
			var title = "Disable requireRecognizedTypes on ApiSurface";
			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
						context.Document,
						configurationSource,
						document => TryDisableRequireRecognizedTypes(document, xmlLineNumber, xmlLinePosition),
						cancellationToken),
					title),
				diagnostic);
			return;
		}

		if (string.IsNullOrWhiteSpace(dependencyLayerName) || string.Equals(dependencyLayerName, UnrecognizedLayerName, StringComparison.Ordinal))
		{
			return;
		}

		if (TryFindBlockedLayerRule(policyElement, dependencyLayerName, site, out var blockedRule))
		{
			RegisterBlockedLayerRelaxation(context, diagnostic, configurationSource, blockedRule, dependencyLayerName, site);
			return;
		}

		if (TryFindAllowedLayerRule(policyElement, dependencyLayerName, out var allowedRule)
		    && !ConfigurationCodeFixSupport.SiteFilterAllows(allowedRule, site))
		{
			RegisterAllowedLayerSiteFix(context, diagnostic, configurationSource, allowedRule, dependencyLayerName, site);
			return;
		}

		RegisterAllowedLayerAddition(context, diagnostic, configurationSource, dependencyLayerName, site, xmlLineNumber, xmlLinePosition);
	}

	private static void RegisterBlockedLayerRelaxation(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		XElement blockedRule,
		string dependencyLayerName,
		string site)
	{
		var lineInfo = (System.Xml.IXmlLineInfo)blockedRule;
		if (!lineInfo.HasLineInfo() || string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var allowedSites = ConfigurationCodeFixSupport.ReadSites(blockedRule.Attribute("allowedSites")?.Value);
		if (allowedSites.Count > 0)
		{
			var title = allowedSites.Count == 1
				? $"Remove blocked API-surface rule for '/{dependencyLayerName}' at {site}"
				: $"Stop blocking API-surface layer '/{dependencyLayerName}' at {site}";
			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
						context.Document,
						configurationSource,
						document => TryRelaxBlockedLayerByAllowedSites(document, lineInfo.LineNumber, lineInfo.LinePosition, site),
						cancellationToken),
					title),
				diagnostic);
			return;
		}

		var title2 = $"Stop blocking API-surface layer '/{dependencyLayerName}' at {site}";
		context.RegisterCodeFix(
			CodeAction.Create(
				title2,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryRelaxBlockedLayerByBlockedSites(document, lineInfo.LineNumber, lineInfo.LinePosition, site),
					cancellationToken),
				title2),
			diagnostic);
	}

	private static void RegisterAllowedLayerSiteFix(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		XElement allowedRule,
		string dependencyLayerName,
		string site)
	{
		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var lineInfo = (System.Xml.IXmlLineInfo)allowedRule;
		if (!lineInfo.HasLineInfo())
		{
			return;
		}

		var allowedSites = ConfigurationCodeFixSupport.ReadSites(allowedRule.Attribute("allowedSites")?.Value);
		if (allowedSites.Count > 0 && !allowedSites.Contains(site))
		{
			var title = $"Add site '{site}' to ApiSurface AllowedLayer '/{dependencyLayerName}'";
			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
						context.Document,
						configurationSource,
						document => TryAppendAllowedLayerSite(document, lineInfo.LineNumber, lineInfo.LinePosition, site),
						cancellationToken),
					title),
				diagnostic);
			return;
		}

		var blockedSites = ConfigurationCodeFixSupport.ReadSites(allowedRule.Attribute("blockedSites")?.Value);
		if (!blockedSites.Contains(site))
		{
			return;
		}

		var title2 = $"Remove site '{site}' from ApiSurface blockedSites for '/{dependencyLayerName}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title2,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryRemoveAllowedLayerBlockedSite(document, lineInfo.LineNumber, lineInfo.LinePosition, site),
					cancellationToken),
				title2),
			diagnostic);
	}

	private static void RegisterAllowedLayerAddition(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		string dependencyLayerName,
		string site,
		int xmlLineNumber,
		int xmlLinePosition)
	{
		var broadTitle = $"Allow API surface to expose '/{dependencyLayerName}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				broadTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedLayer(document, xmlLineNumber, xmlLinePosition, dependencyLayerName, null),
					cancellationToken),
				broadTitle),
			diagnostic);

		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var scopedTitle = $"Allow API surface to expose '/{dependencyLayerName}' at {site}";
		context.RegisterCodeFix(
			CodeAction.Create(
				scopedTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedLayer(document, xmlLineNumber, xmlLinePosition, dependencyLayerName, site),
					cancellationToken),
				scopedTitle),
			diagnostic);
	}

	private static bool TryFindBlockedLayerRule(XElement policyElement, string dependencyLayerName, string site, out XElement rule)
	{
		foreach (var candidate in policyElement.Elements("BlockedLayer"))
		{
			if (!RuleTargetsLayer(candidate, dependencyLayerName) || !ConfigurationCodeFixSupport.SiteFilterAllows(candidate, site))
			{
				continue;
			}

			rule = candidate;
			return true;
		}

		rule = null!;
		return false;
	}

	private static bool TryFindAllowedLayerRule(XElement policyElement, string dependencyLayerName, out XElement rule)
	{
		foreach (var candidate in policyElement.Elements("AllowedLayer"))
		{
			if (!RuleTargetsLayer(candidate, dependencyLayerName))
			{
				continue;
			}

			rule = candidate;
			return true;
		}

		rule = null!;
		return false;
	}

	private static bool RuleTargetsLayer(XElement element, string dependencyLayerName)
	{
		var configuredPath = element.Attribute("path")?.Value;
		if (string.IsNullOrWhiteSpace(configuredPath))
		{
			return false;
		}

		var normalizedPath = configuredPath!.TrimStart('/');
		var result = string.Equals(normalizedPath, dependencyLayerName, StringComparison.Ordinal)
		             || dependencyLayerName.StartsWith(normalizedPath + "/", StringComparison.Ordinal);

		return result;
	}

	private static bool TryDisableRequireRecognizedTypes(XDocument document, int xmlLineNumber, int xmlLinePosition)
	{
		var policyElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "ApiSurface", xmlLineNumber, xmlLinePosition);
		if (policyElement is null)
		{
			return false;
		}

		policyElement.SetAttributeValue("requireRecognizedTypes", "false");
		return true;
	}

	private static bool TryAddAllowedLayer(XDocument document, int xmlLineNumber, int xmlLinePosition, string dependencyLayerName, string? site)
	{
		var policyElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "ApiSurface", xmlLineNumber, xmlLinePosition);
		if (policyElement is null)
		{
			return false;
		}

		var existing = policyElement.Elements("AllowedLayer")
			.FirstOrDefault(element => RuleTargetsLayer(element, dependencyLayerName));
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

		var element = new XElement("AllowedLayer", new XAttribute("path", "/" + dependencyLayerName));
		if (!string.IsNullOrWhiteSpace(site))
		{
			element.SetAttributeValue("allowedSites", site);
		}

		policyElement.Add(element);
		return true;
	}

	private static bool TryAppendAllowedLayerSite(XDocument document, int xmlLineNumber, int xmlLinePosition, string site)
	{
		var rule = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "AllowedLayer", xmlLineNumber, xmlLinePosition);
		if (rule is null)
		{
			return false;
		}

		var allowedSites = ConfigurationCodeFixSupport.ReadSites(rule.Attribute("allowedSites")?.Value);
		if (!allowedSites.Add(site))
		{
			return false;
		}

		rule.SetAttributeValue("allowedSites", ConfigurationCodeFixSupport.FormatSites(allowedSites));
		return true;
	}

	private static bool TryRemoveAllowedLayerBlockedSite(XDocument document, int xmlLineNumber, int xmlLinePosition, string site)
	{
		var rule = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "AllowedLayer", xmlLineNumber, xmlLinePosition);
		if (rule is null)
		{
			return false;
		}

		var blockedSites = ConfigurationCodeFixSupport.ReadSites(rule.Attribute("blockedSites")?.Value);
		if (!blockedSites.Remove(site))
		{
			return false;
		}

		rule.SetAttributeValue("blockedSites", blockedSites.Count == 0 ? null : ConfigurationCodeFixSupport.FormatSites(blockedSites));
		return true;
	}

	private static bool TryRelaxBlockedLayerByAllowedSites(XDocument document, int xmlLineNumber, int xmlLinePosition, string site)
	{
		var rule = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "BlockedLayer", xmlLineNumber, xmlLinePosition);
		if (rule is null)
		{
			return false;
		}

		var allowedSites = ConfigurationCodeFixSupport.ReadSites(rule.Attribute("allowedSites")?.Value);
		if (!allowedSites.Remove(site))
		{
			return false;
		}

		if (allowedSites.Count == 0)
		{
			rule.Remove();
			return true;
		}

		rule.SetAttributeValue("allowedSites", ConfigurationCodeFixSupport.FormatSites(allowedSites));
		return true;
	}

	private static bool TryRelaxBlockedLayerByBlockedSites(XDocument document, int xmlLineNumber, int xmlLinePosition, string site)
	{
		var rule = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "BlockedLayer", xmlLineNumber, xmlLinePosition);
		if (rule is null)
		{
			return false;
		}

		var blockedSites = ConfigurationCodeFixSupport.ReadSites(rule.Attribute("blockedSites")?.Value);
		if (blockedSites.Contains(site))
		{
			return false;
		}

		blockedSites.Add(site);
		rule.SetAttributeValue("blockedSites", ConfigurationCodeFixSupport.FormatSites(blockedSites));
		return true;
	}
}
