using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class ProjectArchitectureCodeFix
{
	private const string MissingAllowedPrefix = "no AllowedProjectReference permits project group '";
	private const string SameGroupPrefix = "same-group reference from '";
	private const string BlockedPrefix = "BlockedProjectReference from '";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var sourceProjectGroup = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertySourceProjectGroup);
		var targetProjectGroup = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyTargetProjectGroup);
		var violationReason = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyViolationReason);
		if (string.IsNullOrWhiteSpace(violationReason))
		{
			return;
		}

		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		if (snapshots.IsDefaultOrEmpty)
		{
			return;
		}

		if (TryParseBlockedRule(violationReason, out var blockedFrom, out var blockedTo))
		{
			await TryRegisterBlockedRuleRemovalAsync(context, diagnostic, snapshots, blockedFrom, blockedTo).ConfigureAwait(false);
			return;
		}

		if (string.IsNullOrWhiteSpace(sourceProjectGroup)
		    || string.IsNullOrWhiteSpace(targetProjectGroup)
		    || string.Equals(sourceProjectGroup, "unrecognized", StringComparison.OrdinalIgnoreCase)
		    || string.Equals(targetProjectGroup, "unrecognized", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		if (!IsMissingAllowedRule(violationReason, sourceProjectGroup, targetProjectGroup)
		    && !IsMissingSelfEdge(violationReason, sourceProjectGroup, targetProjectGroup))
		{
			return;
		}

		var discoveredSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var configurationSource = ResolveProjectArchitectureSource(discoveredSource, string.Empty, snapshots);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		var title = string.Equals(sourceProjectGroup, targetProjectGroup, StringComparison.Ordinal)
			? $"Allow project group '{sourceProjectGroup}' to reference itself"
			: $"Allow project group '{sourceProjectGroup}' to reference '{targetProjectGroup}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedProjectReference(document, sourceProjectGroup, targetProjectGroup),
					cancellationToken),
				title),
			diagnostic);
	}

	private static async Task TryRegisterBlockedRuleRemovalAsync(
		CodeFixContext context,
		Diagnostic diagnostic,
		System.Collections.Immutable.ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots,
		string blockedFrom,
		string blockedTo)
	{
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlPath);
		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);
		if (string.IsNullOrWhiteSpace(sourcePath) || xmlLineNumber <= 0)
		{
			return;
		}

		var discoveredSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var configurationSource = ResolveProjectArchitectureSource(discoveredSource, sourcePath, snapshots);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		var title = $"Remove blocking <BlockedProjectReference from=\"{blockedFrom}\" to=\"{blockedTo}\" />";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryRemoveBlockedProjectReference(document, xmlLineNumber, xmlLinePosition),
					cancellationToken),
				title),
			diagnostic);
	}

	private static bool TryAddAllowedProjectReference(XDocument document, string sourceProjectGroup, string targetProjectGroup)
	{
		var projectArchitecture = FindProjectArchitectureElement(document);
		if (projectArchitecture is null)
		{
			return false;
		}

		var existingAllowed = projectArchitecture.Elements("AllowedProjectReference")
			.Any(element =>
				string.Equals(element.Attribute("from")?.Value, sourceProjectGroup, StringComparison.Ordinal)
				&& string.Equals(element.Attribute("to")?.Value, targetProjectGroup, StringComparison.Ordinal));
		if (existingAllowed)
		{
			return false;
		}

		var newElement = new XElement(
			"AllowedProjectReference",
			new XAttribute("from", sourceProjectGroup),
			new XAttribute("to", targetProjectGroup));

		var packagePolicy = projectArchitecture.Elements("PackagePolicy").FirstOrDefault();
		if (packagePolicy is not null)
		{
			packagePolicy.AddBeforeSelf(newElement);
			return true;
		}

		var lastRule = projectArchitecture.Elements()
			.LastOrDefault(element =>
				element.Name.LocalName == "AllowedProjectReference"
				|| element.Name.LocalName == "BlockedProjectReference");
		if (lastRule is not null)
		{
			lastRule.AddAfterSelf(newElement);
			return true;
		}

		projectArchitecture.Add(newElement);
		return true;
	}

	private static bool TryRemoveBlockedProjectReference(XDocument document, int xmlLineNumber, int xmlLinePosition)
	{
		var blockedRule = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "BlockedProjectReference", xmlLineNumber, xmlLinePosition);
		if (blockedRule is null)
		{
			return false;
		}

		blockedRule.Remove();
		return true;
	}

	private static RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource ResolveProjectArchitectureSource(
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource discoveredSource,
		string sourcePath,
		System.Collections.Immutable.ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots)
	{
		if (!string.IsNullOrWhiteSpace(sourcePath))
		{
			var resolvedSource = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, sourcePath, snapshots);

			return resolvedSource;
		}

		foreach (var snapshot in snapshots)
		{
			if (FindProjectArchitectureElement(snapshot.Document) is null)
			{
				continue;
			}

			return snapshot.Source;
		}

		var result = discoveredSource;

		return result;
	}

	private static XElement? FindProjectArchitectureElement(XDocument document)
	{
		if (document.Root is null)
		{
			return null;
		}

		if (document.Root.Name.LocalName == "ProjectArchitecture")
		{
			return document.Root;
		}

		var result = document.Root.Element("ProjectArchitecture");

		return result;
	}

	private static bool TryParseBlockedRule(string violationReason, out string blockedFrom, out string blockedTo)
	{
		blockedFrom = string.Empty;
		blockedTo = string.Empty;
		if (!violationReason.StartsWith(BlockedPrefix, StringComparison.Ordinal))
		{
			return false;
		}

		var trimmed = violationReason.Substring(BlockedPrefix.Length);
		var separator = "' to '";
		var separatorIndex = trimmed.IndexOf(separator, StringComparison.Ordinal);
		if (separatorIndex < 0)
		{
			return false;
		}

		var toSuffix = "' denies this project edge";
		if (!trimmed.EndsWith(toSuffix, StringComparison.Ordinal))
		{
			return false;
		}

		blockedFrom = trimmed.Substring(0, separatorIndex);
		blockedTo = trimmed.Substring(separatorIndex + separator.Length, trimmed.Length - separatorIndex - separator.Length - toSuffix.Length);
		var result = blockedFrom.Length > 0 && blockedTo.Length > 0;

		return result;
	}

	private static bool IsMissingAllowedRule(string violationReason, string sourceProjectGroup, string targetProjectGroup)
	{
		var expected = $"{MissingAllowedPrefix}{sourceProjectGroup}' to reference project group '{targetProjectGroup}'";
		var result = string.Equals(violationReason, expected, StringComparison.Ordinal);

		return result;
	}

	private static bool IsMissingSelfEdge(string violationReason, string sourceProjectGroup, string targetProjectGroup)
	{
		if (!string.Equals(sourceProjectGroup, targetProjectGroup, StringComparison.Ordinal))
		{
			return false;
		}

		var expected = $"{SameGroupPrefix}{sourceProjectGroup}' to '{targetProjectGroup}' requires an explicit self-edge";
		var result = string.Equals(violationReason, expected, StringComparison.Ordinal);

		return result;
	}
}
