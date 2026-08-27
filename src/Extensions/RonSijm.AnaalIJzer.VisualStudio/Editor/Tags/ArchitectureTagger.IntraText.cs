using System.Globalization;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Tags;

internal sealed partial class ArchitectureTagger
{
	IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !_snapshot.HasConfiguration || _snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		var options = ArchitectureVisualStudioOptions.Current;
		if (options.EnableInlineLayerBadges)
		{
			foreach (var indicator in GetLayerBadgeIndicators(_snapshot, options))
			{
				if (TryCreatePointSpan(spans[0].Snapshot, indicator.IdentifierSpan.End, out var span))
				{
					var adornment = ArchitectureAdornmentFactory.CreateLayerBadge(indicator);
					yield return new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
				}
			}
		}

		if (options is { EnableSitesDiagnostics: false, EnableSiteLayerInformation: false })
		{
			yield break;
		}

		foreach (var indicator in GetDistinctSiteIndicators(_snapshot))
		{
			if (options.EnableSitesDiagnostics && options.IsSiteDiagnosticEnabled(indicator.Site))
			{
				if (TryCreatePointSpan(spans[0].Snapshot, indicator.Span.End, out var siteSpan))
				{
					var adornment = ArchitectureAdornmentFactory.CreateSiteBadge(indicator);
					yield return new TagSpan<IntraTextAdornmentTag>(siteSpan, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
				}
			}

			if (CanShowSiteLayerInformation(options, indicator) && TryCreatePointSpan(spans[0].Snapshot, indicator.Span.End, out var layerSpan))
			{
				var adornment = ArchitectureAdornmentFactory.CreateSiteLayerBadge(indicator);
				yield return new TagSpan<IntraTextAdornmentTag>(layerSpan, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
			}
		}

		if (options.EnableSitesDiagnostics)
		{
			foreach (var indicator in _snapshot.ApiSurfaceIndicators)
			{
				if (TryCreatePointSpan(spans[0].Snapshot, indicator.Span.End, out var span))
				{
					var adornment = ArchitectureAdornmentFactory.CreateApiSurfaceBadge(indicator);
					yield return new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
				}
			}

			foreach (var indicator in _snapshot.VisibilityPolicyIndicators)
			{
				if (TryCreatePointSpan(spans[0].Snapshot, indicator.Span.End, out var span))
				{
					var adornment = ArchitectureAdornmentFactory.CreateVisibilityPolicyBadge(indicator);
					yield return new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
				}
			}

			foreach (var indicator in _snapshot.NameRuleIndicators)
			{
				if (options.IsSiteDiagnosticEnabled(indicator.Site)
				    && TryCreatePointSpan(spans[0].Snapshot, indicator.Span.End, out var span))
				{
					var adornment = ArchitectureAdornmentFactory.CreateNameRuleBadge(indicator);
					yield return new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
				}
			}
		}
	}

	private static IEnumerable<ArchitectureLayerIndicator> GetLayerBadgeIndicators(ArchitectureEditorSnapshot editorSnapshot, ArchitectureEditorOptions options)
	{
		foreach (var indicator in editorSnapshot.LayerIndicators)
		{
			yield return indicator;
		}

		if (!options.ShowLayerBadgesWhenNotInLayer)
		{
			yield break;
		}

		foreach (var indicator in editorSnapshot.UnclassifiedTypeIndicators)
		{
			yield return indicator;
		}
	}

	private static IEnumerable<ArchitectureDependencySiteIndicator> GetDistinctSiteIndicators(ArchitectureEditorSnapshot editorSnapshot)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var indicator in editorSnapshot.SiteIndicators)
		{
			var key = indicator.Span.Start.ToString(CultureInfo.InvariantCulture)
			          + ":"
			          + indicator.Span.End.ToString(CultureInfo.InvariantCulture)
			          + ":"
			          + indicator.Site
			          + ":"
			          + indicator.DependencyTypeName;
			if (seen.Add(key))
			{
				yield return indicator;
			}
		}
	}

	private static bool CanShowSiteLayerInformation(ArchitectureEditorOptions options, ArchitectureDependencySiteIndicator indicator)
	{
		var result = options.EnableSiteLayerInformation
		             && options.IsSiteLayerInformationEnabled(indicator.Site)
		             && !string.IsNullOrWhiteSpace(indicator.DependencyLayerPath)
		             && indicator.DependencyLayerPaletteSlot > 0;

		return result;
	}
}
