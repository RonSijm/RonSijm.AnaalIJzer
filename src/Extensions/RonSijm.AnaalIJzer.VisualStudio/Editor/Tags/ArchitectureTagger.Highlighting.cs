using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Styling;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Tags;

internal sealed partial class ArchitectureTagger
{
	IEnumerable<ITagSpan<TextMarkerTag>> ITagger<TextMarkerTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !ArchitectureVisualStudioOptions.Current.EnableLayerTextBackgroundTint || !_snapshot.HasConfiguration || _snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		foreach (var indicator in _snapshot.LayerIndicators)
		{
			if (!indicator.IsInLayer)
			{
				continue;
			}

			if (TryCreateFullLineSourceSpan(spans[0].Snapshot, indicator.DeclarationSpan, out var span))
			{
				yield return new TagSpan<TextMarkerTag>(span, new TextMarkerTag(ArchitectureClassificationNames.GetLayerTintName(indicator.PaletteSlot)));
			}
		}
	}

	IEnumerable<ITagSpan<ArchitectureLayerGlyphTag>> ITagger<ArchitectureLayerGlyphTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !ArchitectureVisualStudioOptions.Current.EnableLayerGlyphs || !_snapshot.HasConfiguration || _snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		foreach (var indicator in _snapshot.LayerIndicators)
		{
			if (!indicator.IsInLayer)
			{
				continue;
			}

			if (TryCreateSourceSpan(spans[0].Snapshot, indicator.IdentifierSpan, out var span))
			{
				yield return new TagSpan<ArchitectureLayerGlyphTag>(span, new ArchitectureLayerGlyphTag(indicator));
			}
		}
	}
}
