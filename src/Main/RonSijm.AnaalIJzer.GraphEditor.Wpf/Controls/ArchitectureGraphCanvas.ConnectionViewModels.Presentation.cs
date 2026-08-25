using System.Collections.Immutable;
using System.ComponentModel;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphConnectionViewModel
	{
		private void RefreshSitePresentation()
		{
			foreach (var option in AllowedSiteOptions)
			{
				option.IsChecked = allowedSites.Contains(option.Site, StringComparer.Ordinal);
			}

			foreach (var option in BlockedSiteOptions)
			{
				option.IsChecked = blockedSites.Contains(option.Site, StringComparer.Ordinal);
			}

			var siteText = FormatSiteText(allowedSites, blockedSites);
			LabelText = FormatLabelText(siteText, AppliesToDescendants);
			ToolTip = FormatEdgeToolTip(Kind, From, To, siteText, AppliesToDescendants);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsesAllSites)));
		}

		private static ImmutableArray<string> ToggleSite(ImmutableArray<string> currentSites, string site)
		{
			var builder = ImmutableArray.CreateBuilder<string>();
			builder.AddRange(currentSites.Where(current => !string.Equals(current, site, StringComparison.Ordinal)));
			if (builder.Count == currentSites.Length)
			{
				builder.Add(site);
			}

			var result = ArchitectureDependencySiteNames.All.Where(builder.Contains).ToImmutableArray();

			return result;
		}

		private static string FormatLabelText(string siteText, bool appliesToDescendants)
		{
			var result = siteText + (appliesToDescendants ? ", cascades" : string.Empty);

			return result;
		}

		private static string FormatSiteText(ImmutableArray<string> allowedSites, ImmutableArray<string> blockedSites)
		{
			if (allowedSites.Length > 0)
			{
				return "allowed sites: " + string.Join(", ", allowedSites);
			}

			if (blockedSites.Length > 0)
			{
				return "blocked sites: " + string.Join(", ", blockedSites);
			}

			return "all sites";
		}

		private static string FormatEdgeToolTip(string kind, string from, string to, string siteText, bool appliesToDescendants)
		{
			var cascade = appliesToDescendants ? Environment.NewLine + "Applies to descendants." : string.Empty;
			var result = kind + ": " + from + " -> " + to + Environment.NewLine + siteText + cascade;

			return result;
		}

		private static string FormatEvidenceToolTip(string from, string to, string siteText, string details)
		{
			var detailText = string.IsNullOrWhiteSpace(details) ? string.Empty : Environment.NewLine + details;
			var result = "Observed code dependency: " + from + " -> " + to + Environment.NewLine + siteText + detailText;

			return result;
		}
	}
}
