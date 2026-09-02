using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class VisibilityPolicyCodeFix
{
	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var declaredAccessibility = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDeclaredAccessibility);
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlPath);
		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);
		if (!ConfigurationCodeFixSupport.TryNormalizeAccessibility(declaredAccessibility, out var normalizedAccessibility)
		    || xmlLineNumber <= 0
		    || string.IsNullOrWhiteSpace(sourcePath))
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

		var policyElement = ConfigurationCodeFixSupport.FindElementByLineInfo(snapshot.Document, "VisibilityPolicy", xmlLineNumber, xmlLinePosition);
		if (policyElement is null)
		{
			return;
		}

		if (policyElement.Attribute("allowedAccessibilities") is not null)
		{
			var title = $"Allow visibility '{normalizedAccessibility}' in VisibilityPolicy";
			context.RegisterCodeFix(
				CodeAction.Create(
					title,
					cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
						context.Document,
						configurationSource,
						document => TryAllowAccessibility(document, xmlLineNumber, xmlLinePosition, normalizedAccessibility),
						cancellationToken),
					title),
				diagnostic);
			return;
		}

		var blockedAccessibilities = ConfigurationCodeFixSupport.ReadAccessibilities(policyElement.Attribute("blockedAccessibilities")?.Value);
		if (!blockedAccessibilities.Contains(normalizedAccessibility))
		{
			return;
		}

		var title2 = blockedAccessibilities.Count == 1
			? $"Remove VisibilityPolicy that only blocks '{normalizedAccessibility}'"
			: $"Remove visibility '{normalizedAccessibility}' from blockedAccessibilities";
		context.RegisterCodeFix(
			CodeAction.Create(
				title2,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryRelaxBlockedAccessibility(document, xmlLineNumber, xmlLinePosition, normalizedAccessibility),
					cancellationToken),
				title2),
			diagnostic);
	}

	private static bool TryAllowAccessibility(XDocument document, int xmlLineNumber, int xmlLinePosition, string declaredAccessibility)
	{
		var policyElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "VisibilityPolicy", xmlLineNumber, xmlLinePosition);
		if (policyElement is null)
		{
			return false;
		}

		var accessibilities = ConfigurationCodeFixSupport.ReadAccessibilities(policyElement.Attribute("allowedAccessibilities")?.Value);
		if (!accessibilities.Add(declaredAccessibility))
		{
			return false;
		}

		policyElement.SetAttributeValue("allowedAccessibilities", ConfigurationCodeFixSupport.FormatAccessibilities(accessibilities));
		return true;
	}

	private static bool TryRelaxBlockedAccessibility(XDocument document, int xmlLineNumber, int xmlLinePosition, string declaredAccessibility)
	{
		var policyElement = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "VisibilityPolicy", xmlLineNumber, xmlLinePosition);
		if (policyElement is null)
		{
			return false;
		}

		var blockedAccessibilities = ConfigurationCodeFixSupport.ReadAccessibilities(policyElement.Attribute("blockedAccessibilities")?.Value);
		if (!blockedAccessibilities.Remove(declaredAccessibility))
		{
			return false;
		}

		if (blockedAccessibilities.Count == 0)
		{
			policyElement.Remove();
			return true;
		}

		policyElement.SetAttributeValue("blockedAccessibilities", ConfigurationCodeFixSupport.FormatAccessibilities(blockedAccessibilities));
		return true;
	}
}
