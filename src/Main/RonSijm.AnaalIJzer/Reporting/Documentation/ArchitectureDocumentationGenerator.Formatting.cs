using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.DependencyRules;
using RonSijm.AnaalIJzer.Model;

namespace RonSijm.AnaalIJzer.Documentation;

internal static partial class ArchitectureDocumentationGenerator
{
	private static bool SiteAttributesMatch(ArchitectureDocumentationItem item, DependencyEdge edge)
	{
		var appliesToDescendants = item.GetAttribute("appliesToDescendants");
		if (edge.AppliesToDescendants != IsTrueAttribute(appliesToDescendants))
		{
			return false;
		}

		var allowedSites = item.GetAttribute("allowedSites");
		var blockedSites = item.GetAttribute("blockedSites");
		if (!edge.SiteFilter.HasFilter)
		{
			return allowedSites is null && blockedSites is null;
		}

		return allowedSites is not null && SitesMatch(allowedSites, edge.SiteFilter.AllowedSites)
		       || blockedSites is not null && SitesMatch(blockedSites, edge.SiteFilter.BlockedSites);
	}

	private static bool IsTrueAttribute(string? value)
	{
		var result = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

		return result;
	}

	private static bool SitesMatch(string text, ImmutableHashSet<string> sites)
	{
		var parsedSites = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
		foreach (var rawToken in text.Split(','))
		{
			var token = rawToken.Trim();
			if (token.Length == 0)
			{
				continue;
			}

			if (!DependencySites.TryNormalize(token, out var normalized))
			{
				return false;
			}

			parsedSites.Add(normalized);
		}

		return parsedSites.SetEquals(sites);
	}

	private static string FormatAttributes(ImmutableArray<ArchitectureDocumentationAttribute> attributes)
	{
		if (attributes.Length == 0)
		{
			return string.Empty;
		}

		return string.Join(" ", attributes.Select(attribute => $"{attribute.Name}=\"{attribute.Value}\""));
	}

	private static string LayerId(string name)
	{
		var result = "L_" + Sanitize(name);

		return result;
	}

	private static string SubgraphId(string name)
	{
		var result = "SG_" + Sanitize(name);

		return result;
	}

	private static string GetRootName(string path)
	{
		var separator = path.IndexOf('/');
		return separator < 0 ? path : path.Substring(0, separator);
	}

	private static string GetLocalName(string path)
	{
		var separator = path.LastIndexOf('/');
		return separator < 0 ? path : path.Substring(separator + 1);
	}

	private static string Sanitize(string name)
	{
		var sb = new StringBuilder(name.Length);
		foreach (var c in name)
		{
			sb.Append(IsIdChar(c) ? c : '_');
		}

		return sb.ToString();
	}

	private static bool IsIdChar(char c)
	{
		var result = c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';

		return result;
	}

    private static string EscapeLabel(string text)
    {
        return text.Replace("\"", "&quot;").Replace("|", "&#124;");
    }

	private static string EscapeTable(string text)
	{
		var result = text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

		return result;
	}

	private static string EscapeMarkdown(string text)
	{
		var result = text.Replace("\r", " ").Replace("\n", " ");

		return result;
	}
}
