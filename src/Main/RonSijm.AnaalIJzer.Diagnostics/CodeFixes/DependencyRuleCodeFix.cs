using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class DependencyRuleCodeFix
{
	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		if (!TryReadLayersAndSite(diagnostic, out var callerLayer, out var dependencyLayer, out var site))
		{
			return;
		}

		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var configurationSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		if (TryReadSiteFilterFix(diagnostic, configurationSource, snapshots, out var siteFilterFix))
		{
			RegisterSiteFilterFix(context, diagnostic, siteFilterFix, site);
			return;
		}

		var denialKind = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyDenialKind);
		if (string.Equals(denialKind, "BlockedEdge", StringComparison.Ordinal))
		{
			return;
		}

		if (diagnostic.Id is ArchitecturalDiagnosticIds.IllegalLevelDependency or ArchitecturalDiagnosticIds.WrongDirectionDependency)
		{
			RegisterMissingAllowedDependencyFix(context, diagnostic, configurationSource, callerLayer, dependencyLayer);
		}

		if (diagnostic.Id == ArchitecturalDiagnosticIds.WrongDirectionDependency
		    && TryReadReverseDependencyFlipFix(diagnostic, configurationSource, snapshots, out var reverseDependencyFix))
		{
			RegisterReverseDependencyFlipFix(context, diagnostic, reverseDependencyFix);
		}

		if (diagnostic.Id == ArchitecturalDiagnosticIds.SameLayerDependency)
		{
			RegisterSameLayerFixes(context, diagnostic, configurationSource, callerLayer, site);
		}
	}

	private static void RegisterMissingAllowedDependencyFix(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		string callerLayer,
		string dependencyLayer)
	{
		var title = $"Add allowed dependency '{callerLayer}' -> '{dependencyLayer}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedDependency(document, callerLayer, dependencyLayer, null),
					cancellationToken),
				title),
			diagnostic);
	}

	private static void RegisterReverseDependencyFlipFix(CodeFixContext context, Diagnostic diagnostic, ReverseDependencyFlipFix fix)
	{
		var title = $"Flip configured dependency '{fix.ConfiguredFrom}' -> '{fix.ConfiguredTo}' to '{fix.ConfiguredTo}' -> '{fix.ConfiguredFrom}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					fix.Source,
					document => TryFlipAllowedDependency(document, fix),
					cancellationToken),
				title),
			diagnostic);
	}

	private static void RegisterSameLayerFixes(
		CodeFixContext context,
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource configurationSource,
		string layerName,
		string site)
	{
		var broadTitle = $"Allow same-layer dependency '{layerName}' -> '{layerName}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				broadTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedDependency(document, layerName, layerName, null),
					cancellationToken),
				broadTitle),
			diagnostic);

		if (string.IsNullOrWhiteSpace(site))
		{
			return;
		}

		var narrowTitle = $"Allow same-layer dependency '{layerName}' -> '{layerName}' at {site}";
		context.RegisterCodeFix(
			CodeAction.Create(
				narrowTitle,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedDependency(document, layerName, layerName, site),
					cancellationToken),
				narrowTitle),
			diagnostic);
	}

	private static void RegisterSiteFilterFix(CodeFixContext context, Diagnostic diagnostic, DependencySiteFilterFix fix, string site)
	{
		var title = fix.Mode == SiteFilterFixMode.AppendAllowedSite
			? $"Add site '{site}' to allowedSites for '{fix.ConfiguredFrom}' -> '{fix.ConfiguredTo}'"
			: $"Remove site '{site}' from blockedSites for '{fix.ConfiguredFrom}' -> '{fix.ConfiguredTo}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					fix.Source,
					document => TryApplySiteFilterFix(document, fix, site),
					cancellationToken),
				title),
			diagnostic);
	}

	private static bool TryAddAllowedDependency(XDocument document, string from, string to, string? site)
	{
		var insertion = CreateDependencyInsertion(from, to);
		var changed = TryInsertAllowedDependency(document, insertion.ScopePath, insertion.ConfiguredFrom, insertion.ConfiguredTo, site);
		if (changed)
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(insertion.ScopePath))
		{
			return false;
		}

		var result = TryInsertAllowedDependency(document, string.Empty, FormatRootLayerReference(from), FormatRootLayerReference(to), site);

		return result;
	}

	private static bool TryInsertAllowedDependency(XDocument document, string scopePath, string configuredFrom, string configuredTo, string? site)
	{
		var container = FindDependencyInsertionContainer(document, scopePath);
		if (container is null || HasMatchingDependency(container, configuredFrom, configuredTo))
		{
			return false;
		}

		var element = new XElement(
			"AllowedDependency",
			new XAttribute("from", configuredFrom),
			new XAttribute("to", configuredTo));
		if (!string.IsNullOrWhiteSpace(site))
		{
			element.SetAttributeValue("allowedSites", site);
		}

		container.Add(element);
		return true;
	}

	private static bool TryApplySiteFilterFix(XDocument document, DependencySiteFilterFix fix, string site)
	{
		var element = FindDependencyElement(document, fix);
		if (element is null)
		{
			return false;
		}

		if (fix.Mode == SiteFilterFixMode.AppendAllowedSite)
		{
			var allowedSites = ConfigurationCodeFixSupport.ReadSites(element.Attribute("allowedSites")?.Value);
			if (!allowedSites.Add(site))
			{
				return false;
			}

			element.SetAttributeValue("allowedSites", ConfigurationCodeFixSupport.FormatSites(allowedSites));
			return true;
		}

		var blockedSites = ConfigurationCodeFixSupport.ReadSites(element.Attribute("blockedSites")?.Value);
		if (!blockedSites.Remove(site))
		{
			return false;
		}

		element.SetAttributeValue(
			"blockedSites",
			blockedSites.Count == 0 ? null : ConfigurationCodeFixSupport.FormatSites(blockedSites));
		return true;
	}

	private static XElement? FindDependencyElement(XDocument document, DependencySiteFilterFix fix)
	{
		var elements = document
			.Descendants()
			.Where(element => string.Equals(element.Name.LocalName, fix.ElementKind, StringComparison.Ordinal))
			.ToArray();
		var byLine = ConfigurationCodeFixSupport.FindElementByLineInfo(elements, fix.XmlLineNumber, fix.XmlLinePosition);
		if (byLine is not null)
		{
			return byLine;
		}

		var byAttributes = elements.FirstOrDefault(element =>
			string.Equals(element.Attribute("from")?.Value, fix.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(element.Attribute("to")?.Value, fix.ConfiguredTo, StringComparison.Ordinal));

		return byAttributes;
	}

	private static bool TryFlipAllowedDependency(XDocument document, ReverseDependencyFlipFix fix)
	{
		var element = FindAllowedDependencyElement(document, fix);
		if (element is null)
		{
			return false;
		}

		var parent = element.Parent;
		if (parent is not null
		    && parent.Elements("AllowedDependency").Any(candidate =>
			    !ReferenceEquals(candidate, element)
			    && string.Equals(candidate.Attribute("from")?.Value, fix.ConfiguredTo, StringComparison.Ordinal)
			    && string.Equals(candidate.Attribute("to")?.Value, fix.ConfiguredFrom, StringComparison.Ordinal)))
		{
			return false;
		}

		element.SetAttributeValue("from", fix.ConfiguredTo);
		element.SetAttributeValue("to", fix.ConfiguredFrom);

		return true;
	}

	private static XElement? FindAllowedDependencyElement(XDocument document, ReverseDependencyFlipFix fix)
	{
		var elements = document.Descendants("AllowedDependency").ToArray();
		var byLine = ConfigurationCodeFixSupport.FindElementByLineInfo(elements, fix.XmlLineNumber, fix.XmlLinePosition);
		if (byLine is not null)
		{
			return byLine;
		}

		var byAttributes = elements.FirstOrDefault(element =>
			string.Equals(element.Attribute("from")?.Value, fix.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(element.Attribute("to")?.Value, fix.ConfiguredTo, StringComparison.Ordinal));

		return byAttributes;
	}

	private static XElement? FindDependencyInsertionContainer(XDocument document, string scopePath)
	{
		if (document.Root is null)
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(scopePath))
		{
			return document.Root;
		}

		var result = ConfigurationCodeFixSupport.FindLayerElement(document, scopePath);

		return result;
	}

	private static bool HasMatchingDependency(XElement container, string from, string to)
	{
		var result = container
			.Elements("AllowedDependency")
			.Any(element => string.Equals(element.Attribute("from")?.Value, from, StringComparison.Ordinal)
			                && string.Equals(element.Attribute("to")?.Value, to, StringComparison.Ordinal));

		return result;
	}

	private static DependencyInsertion CreateDependencyInsertion(string from, string to)
	{
		if (from == "*" || to == "*")
		{
			return new DependencyInsertion(string.Empty, from, to);
		}

		var fromParts = SplitLayerPath(from);
		var toParts = SplitLayerPath(to);
		var commonLength = GetCommonPrefixLength(fromParts, toParts);
		var areDirectSiblings = fromParts.Length == commonLength + 1 && toParts.Length == commonLength + 1;
		if (areDirectSiblings)
		{
			var scopePath = string.Join("/", fromParts.Take(commonLength));
			var siblingResult = new DependencyInsertion(scopePath, fromParts[fromParts.Length - 1], toParts[toParts.Length - 1]);

			return siblingResult;
		}

		var result = new DependencyInsertion(string.Empty, FormatRootLayerReference(from), FormatRootLayerReference(to));

		return result;
	}

	private static string[] SplitLayerPath(string layerPath)
	{
		var result = layerPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

		return result;
	}

	private static int GetCommonPrefixLength(string[] left, string[] right)
	{
		var count = Math.Min(left.Length, right.Length);
		for (var index = 0; index < count; index++)
		{
			if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
			{
				return index;
			}
		}

		return count;
	}

	private static string FormatRootLayerReference(string layerPath)
	{
		if (layerPath == "*" || !layerPath.Contains("/"))
		{
			return layerPath;
		}

		var result = "/" + layerPath;

		return result;
	}

	private static bool TryReadLayersAndSite(Diagnostic diagnostic, out string callerLayer, out string dependencyLayer, out string site)
	{
		callerLayer = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyCallerLayerName);
		dependencyLayer = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDepLayerName);
		site = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertySite);

		var result = !string.IsNullOrWhiteSpace(callerLayer)
		             && !string.IsNullOrWhiteSpace(dependencyLayer);

		return result;
	}

	private static bool TryReadSiteFilterFix(
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource discoveredSource,
		ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots,
		out DependencySiteFilterFix fix)
	{
		fix = default;

		var denialKind = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyDenialKind);
		if (!string.Equals(denialKind, "SiteFilter", StringComparison.Ordinal))
		{
			return false;
		}

		var siteFilterMode = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencySiteFilterMode);
		var elementKind = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyRuleKind);
		var configuredFrom = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyRuleConfiguredFrom);
		var configuredTo = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyRuleConfiguredTo);
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyRuleXmlPath);
		if (string.IsNullOrWhiteSpace(siteFilterMode)
		    || string.IsNullOrWhiteSpace(elementKind)
		    || string.IsNullOrWhiteSpace(configuredFrom)
		    || string.IsNullOrWhiteSpace(configuredTo)
		    || string.IsNullOrWhiteSpace(sourcePath))
		{
			return false;
		}

		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyDependencyRuleXmlCol);
		var source = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, sourcePath, snapshots);
		var mode = string.Equals(siteFilterMode, "AllowedSites", StringComparison.Ordinal)
			? SiteFilterFixMode.AppendAllowedSite
			: string.Equals(siteFilterMode, "BlockedSites", StringComparison.Ordinal)
				? SiteFilterFixMode.RemoveBlockedSite
				: SiteFilterFixMode.None;
		if (mode == SiteFilterFixMode.None)
		{
			return false;
		}

		fix = new DependencySiteFilterFix(source, elementKind, configuredFrom, configuredTo, xmlLineNumber, xmlLinePosition, mode);
		return true;
	}

	private static bool TryReadReverseDependencyFlipFix(
		Diagnostic diagnostic,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource discoveredSource,
		ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots,
		out ReverseDependencyFlipFix fix)
	{
		fix = default;

		var elementKind = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyReverseDependencyRuleKind);
		var configuredFrom = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyReverseDependencyRuleConfiguredFrom);
		var configuredTo = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyReverseDependencyRuleConfiguredTo);
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyReverseDependencyRuleXmlPath);
		if (!string.Equals(elementKind, "AllowedDependency", StringComparison.Ordinal)
		    || string.IsNullOrWhiteSpace(configuredFrom)
		    || string.IsNullOrWhiteSpace(configuredTo)
		    || string.IsNullOrWhiteSpace(sourcePath)
		    || configuredFrom == "*"
		    || configuredTo == "*")
		{
			return false;
		}

		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyReverseDependencyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyReverseDependencyRuleXmlCol);
		var source = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, sourcePath, snapshots);
		fix = new ReverseDependencyFlipFix(source, configuredFrom, configuredTo, xmlLineNumber, xmlLinePosition);

		return true;
	}

	private readonly struct DependencyInsertion(string scopePath, string configuredFrom, string configuredTo)
	{
		public string ScopePath { get; } = scopePath;

		public string ConfiguredFrom { get; } = configuredFrom;

		public string ConfiguredTo { get; } = configuredTo;
	}

	private readonly struct DependencySiteFilterFix(
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource source,
		string elementKind,
		string configuredFrom,
		string configuredTo,
		int xmlLineNumber,
		int xmlLinePosition,
		SiteFilterFixMode mode)
	{
		public RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource Source { get; } = source;

		public string ElementKind { get; } = elementKind;

		public string ConfiguredFrom { get; } = configuredFrom;

		public string ConfiguredTo { get; } = configuredTo;

		public int XmlLineNumber { get; } = xmlLineNumber;

		public int XmlLinePosition { get; } = xmlLinePosition;

		public SiteFilterFixMode Mode { get; } = mode;
	}

	private readonly struct ReverseDependencyFlipFix(
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource source,
		string configuredFrom,
		string configuredTo,
		int xmlLineNumber,
		int xmlLinePosition)
	{
		public RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource Source { get; } = source;

		public string ConfiguredFrom { get; } = configuredFrom;

		public string ConfiguredTo { get; } = configuredTo;

		public int XmlLineNumber { get; } = xmlLineNumber;

		public int XmlLinePosition { get; } = xmlLinePosition;
	}

	private enum SiteFilterFixMode
	{
		None,
		AppendAllowedSite,
		RemoveBlockedSite,
	}
}
