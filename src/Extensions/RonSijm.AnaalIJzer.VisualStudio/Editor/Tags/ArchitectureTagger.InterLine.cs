using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Tags;

internal sealed partial class ArchitectureTagger
{
	IEnumerable<ITagSpan<InterLineAdornmentTag>> ITagger<InterLineAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !_snapshot.HasConfiguration || _snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		var options = ArchitectureVisualStudioOptions.Current;
		if (!options.EnableLayerCodeLens)
		{
			yield break;
		}

		foreach (var indicator in GetLayerBadgeIndicators(_snapshot, options))
		{
			if (TryCreatePointSpan(spans[0].Snapshot, indicator.DeclarationSpan.Start, out var span))
			{
				var tag = new InterLineAdornmentTag(
					(_, _, _) =>
					{
						Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
						var result = ArchitectureLayerCodeLensAdornment.Create(indicator, options);

						return result;
					},
					true,
					ArchitectureLayerCodeLensAdornment.Height,
					HorizontalPositioningMode.TextRelative,
					0,
					null);
				yield return new TagSpan<InterLineAdornmentTag>(span, tag);
			}
		}
	}
}
