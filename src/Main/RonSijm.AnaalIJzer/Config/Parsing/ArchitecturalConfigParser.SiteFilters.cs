using System.Collections.Immutable;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.DependencyRules;

namespace RonSijm.AnaalIJzer.Parsing;

internal static partial class ArchitecturalConfigParser
{
	private static bool IsEnabled(XElement root, string attributeName)
	{
		var result = string.Equals(root.Attribute(attributeName)?.Value, "true", StringComparison.OrdinalIgnoreCase);

		return result;
	}

	private static bool TryReadBooleanAttribute(XElement element, string attributeName, out bool value)
	{
		var text = element.Attribute(attributeName)?.Value;
		if (text is null)
		{
			value = false;
			return true;
		}

		var trimmed = text.Trim();
		if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
		{
			value = true;
			return true;
		}

		if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) || trimmed == "0")
		{
			value = false;
			return true;
		}

		value = false;
		return false;
	}

	private static bool TryReadSiteFilter(XElement el, out DependencySiteFilter siteFilter, out string error)
	{
		var allowedSitesText = el.Attribute("allowedSites")?.Value;
		var blockedSitesText = el.Attribute("blockedSites")?.Value;

		if (allowedSitesText is not null && blockedSitesText is not null)
		{
			siteFilter = DependencySiteFilter.All;
			error = $"{el.Name.LocalName} may use allowedSites or blockedSites, but not both.";
			return false;
		}

		if (allowedSitesText is not null)
		{
			if (!TryParseSiteList(allowedSitesText, out var allowedSites))
			{
				siteFilter = DependencySiteFilter.All;
				error = $"{el.Name.LocalName} contains an empty or unknown allowedSites value.";
				return false;
			}

			siteFilter = new DependencySiteFilter(allowedSites, ImmutableHashSet<string>.Empty);
			error = string.Empty;
			return true;
		}

		if (blockedSitesText is not null)
		{
			if (!TryParseSiteList(blockedSitesText, out var blockedSites))
			{
				siteFilter = DependencySiteFilter.All;
				error = $"{el.Name.LocalName} contains an empty or unknown blockedSites value.";
				return false;
			}

			siteFilter = new DependencySiteFilter(ImmutableHashSet<string>.Empty, blockedSites);
			error = string.Empty;
			return true;
		}

		siteFilter = DependencySiteFilter.All;
		error = string.Empty;
		return true;
	}

	private static ImmutableHashSet<string> ParseRequiredRecognizedDependencySites(IEnumerable<(XElement Root, string Path)> documents, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var sites = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		foreach (var (root, path) in documents)
		{
			if (ParseRequiredRecognizedDependencySitesAttribute(root, path, "ArchitecturalLevels", issues) is { } documentSites)
			{
				sites.UnionWith(documentSites);
			}
		}

		return sites.ToImmutable();
	}

	private static ImmutableHashSet<string>? ParseRequiredRecognizedDependencySitesAttribute(XElement element, string path, string scopeDescription, ImmutableArray<ConfigurationIssue>.Builder issues)
	{
		var value = element.Attribute("requireRecognizedDependencies")?.Value;
		if (value is null)
		{
			return null;
		}

		if (TryParseSiteList(value, out var sites))
		{
			return sites;
		}

		AddIssue(issues, ConfigurationIssueKind.InvalidConfiguration, $"{scopeDescription} contains an empty or unknown requireRecognizedDependencies value.", element, path);
		return ImmutableHashSet<string>.Empty;
	}

	private static bool TryParseSiteList(string value, out ImmutableHashSet<string> sites)
	{
		var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

		foreach (var rawToken in value.Split(','))
		{
			var token = rawToken.Trim();
			if (token.Length == 0)
			{
				continue;
			}

			if (!DependencySites.TryNormalize(token, out var normalized))
			{
				sites = ImmutableHashSet<string>.Empty;
				return false;
			}

			builder.Add(normalized);
		}

		sites = builder.ToImmutable();
		return sites.Count > 0;
	}

	private static bool TryFindEnabledDocument(List<(XElement Root, string Path)> documents, string attributeName, out XElement? root, out string? path)
	{
		foreach (var document in documents)
		{
			if (!IsEnabled(document.Root, attributeName))
			{
				continue;
			}

			root = document.Root;
			path = document.Path;
			return true;
		}

		root = null;
		path = null;
		return false;
	}
}
