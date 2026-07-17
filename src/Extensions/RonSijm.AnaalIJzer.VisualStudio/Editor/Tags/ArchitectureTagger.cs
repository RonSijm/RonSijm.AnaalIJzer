using System.Globalization;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.LayerIndicators;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Tags;

internal sealed class ArchitectureTagger :
	ITagger<IntraTextAdornmentTag>,
	ITagger<InterLineAdornmentTag>,
	ITagger<TextMarkerTag>,
	ITagger<ArchitectureLayerGlyphTag>,
	IDisposable
{
	private readonly ITextView view;
	private readonly ITextBuffer buffer;
	private readonly ArchitectureSnapshotProvider snapshotProvider;
	private CancellationTokenSource? refreshCancellation;
	private ArchitectureEditorSnapshot snapshot = ArchitectureEditorSnapshot.Empty;

	public ArchitectureTagger(ITextView view, ITextBuffer buffer, ArchitectureSnapshotProvider snapshotProvider)
	{
		this.view = view;
		this.buffer = buffer;
		this.snapshotProvider = snapshotProvider;
		buffer.Changed += BufferChanged;
		view.LayoutChanged += ViewLayoutChanged;
		view.GotAggregateFocus += ViewGotAggregateFocus;
		view.Closed += ViewClosed;
		ArchitectureVisualStudioOptions.Changed += OptionsChanged;
		ArchitectureVisualStudioLog.Info("ArchitectureTagger created for buffer content type '" + buffer.ContentType.TypeName + "'.");
		QueueRefresh(TimeSpan.Zero);
	}

	public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

	IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		var options = ArchitectureVisualStudioOptions.Current;
		if (options.EnableInlineLayerBadges)
		{
			foreach (var indicator in GetLayerBadgeIndicators(snapshot, options))
			{
				if (TryCreatePointSpan(spans[0].Snapshot, indicator.IdentifierSpan.End, out var span))
				{
					var adornment = ArchitectureAdornmentFactory.CreateLayerBadge(indicator);
					yield return new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
				}
			}
		}

		if (!options.EnableSitesDiagnostics && !options.EnableSiteLayerInformation)
		{
			yield break;
		}

		foreach (var indicator in GetDistinctSiteIndicators(snapshot))
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
	}

	IEnumerable<ITagSpan<InterLineAdornmentTag>> ITagger<InterLineAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		var options = ArchitectureVisualStudioOptions.Current;
		if (!options.EnableLayerCodeLens)
		{
			yield break;
		}

		foreach (var indicator in GetLayerBadgeIndicators(snapshot, options))
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

	IEnumerable<ITagSpan<TextMarkerTag>> ITagger<TextMarkerTag>.GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0 || !ArchitectureVisualStudioOptions.Current.EnableLayerTextBackgroundTint || !snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		foreach (var indicator in snapshot.LayerIndicators)
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
		if (spans.Count == 0 || !ArchitectureVisualStudioOptions.Current.EnableLayerGlyphs || !snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			yield break;
		}

		foreach (var indicator in snapshot.LayerIndicators)
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

	public void Dispose()
	{
		refreshCancellation?.Cancel();
		buffer.Changed -= BufferChanged;
		view.LayoutChanged -= ViewLayoutChanged;
		view.GotAggregateFocus -= ViewGotAggregateFocus;
		view.Closed -= ViewClosed;
		ArchitectureVisualStudioOptions.Changed -= OptionsChanged;
	}

	private void BufferChanged(object sender, TextContentChangedEventArgs e)
	{
		QueueRefresh(TimeSpan.FromMilliseconds(250));
	}

	private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
	{
		if (e.NewSnapshot != e.OldSnapshot)
		{
			QueueRefresh(TimeSpan.FromMilliseconds(250));
		}
	}

	private void ViewGotAggregateFocus(object sender, EventArgs e)
	{
		ArchitectureGraphToolWindowState.Publish(snapshot);
	}

	private void ViewClosed(object sender, EventArgs e)
	{
		Dispose();
	}

	private void OptionsChanged(object sender, EventArgs e)
	{
		ArchitectureVisualStudioLog.Info("ArchitectureTagger observed option changes; refreshing tags.");
		RaiseTagsChanged();
	}

	private void QueueRefresh(TimeSpan delay)
	{
		refreshCancellation?.Cancel();
		var cancellation = new CancellationTokenSource();
		refreshCancellation = cancellation;
		_ = RefreshAsync(delay, cancellation.Token);
	}

	private async Task RefreshAsync(TimeSpan delay, CancellationToken cancellationToken)
	{
		try
		{
			if (delay > TimeSpan.Zero)
			{
				await Task.Delay(delay, cancellationToken);
			}

			var result = await snapshotProvider.CreateSnapshotAsync(buffer, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			snapshot = result;
			ArchitectureVisualStudioLog.Info(
				"ArchitectureTagger refreshed. HasConfiguration="
				+ result.HasConfiguration
				+ ", HasConfigurationIssues="
				+ result.HasConfigurationIssues
				+ ", Layers="
				+ result.LayerIndicators.Length
				+ ", Sites="
				+ result.SiteIndicators.Length
				+ ".");
			if (view.HasAggregateFocus)
			{
				ArchitectureGraphToolWindowState.Publish(snapshot);
			}

			RaiseTagsChanged();
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			ArchitectureVisualStudioLog.Exception("ArchitectureTagger refresh failed.", exception);
		}
	}

	private void RaiseTagsChanged()
	{
		var currentSnapshot = buffer.CurrentSnapshot;
		var span = new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length);
		TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
	}

	private static bool TryCreatePointSpan(ITextSnapshot snapshot, int position, out SnapshotSpan span)
	{
		if (position < 0 || position > snapshot.Length)
		{
			span = default;
			return false;
		}

		span = new SnapshotSpan(snapshot, position, 0);
		return true;
	}

	private static bool TryCreateSourceSpan(ITextSnapshot snapshot, Microsoft.CodeAnalysis.Text.TextSpan sourceSpan, out SnapshotSpan span)
	{
		if (sourceSpan.Start < 0 || sourceSpan.End > snapshot.Length)
		{
			span = default;
			return false;
		}

		span = new SnapshotSpan(snapshot, sourceSpan.Start, sourceSpan.Length);
		return true;
	}

	private static bool TryCreateFullLineSourceSpan(ITextSnapshot snapshot, Microsoft.CodeAnalysis.Text.TextSpan sourceSpan, out SnapshotSpan span)
	{
		if (sourceSpan.Start < 0 || sourceSpan.End > snapshot.Length)
		{
			span = default;
			return false;
		}

		var startLine = snapshot.GetLineFromPosition(sourceSpan.Start);
		var endPosition = Math.Max(sourceSpan.Start, sourceSpan.End - 1);
		var endLine = snapshot.GetLineFromPosition(endPosition);
		span = new SnapshotSpan(snapshot, startLine.Start, endLine.EndIncludingLineBreak);
		return true;
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
