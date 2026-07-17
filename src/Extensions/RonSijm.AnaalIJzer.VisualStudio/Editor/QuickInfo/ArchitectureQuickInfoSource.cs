using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.QuickInfo;
using RonSijm.AnaalIJzer.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio;

internal sealed class ArchitectureQuickInfoSource : IAsyncQuickInfoSource
{
	private readonly ITextBuffer buffer;
	private readonly ArchitectureSnapshotProvider snapshotProvider;

	public ArchitectureQuickInfoSource(ITextBuffer buffer, ArchitectureSnapshotProvider snapshotProvider)
	{
		this.buffer = buffer;
		this.snapshotProvider = snapshotProvider;
		ArchitectureVisualStudioLog.Info("ArchitectureQuickInfoSource created for content type '" + buffer.ContentType.TypeName + "'.");
	}

	public void Dispose()
	{
	}

	public async Task<QuickInfoItem?> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
	{
		var triggerPoint = session.GetTriggerPoint(buffer.CurrentSnapshot);
		if (!triggerPoint.HasValue)
		{
			ArchitectureVisualStudioLog.Info("QuickInfo requested without a trigger point.");
			return null;
		}

		var snapshot = await snapshotProvider.CreateSnapshotAsync(buffer, cancellationToken);
		if (!snapshot.HasConfiguration || snapshot.HasConfigurationIssues)
		{
			ArchitectureVisualStudioLog.Info("QuickInfo suppressed because snapshot has no valid configuration.");
			return null;
		}

		var position = triggerPoint.Value.Position;
		var item = TryCreateSiteQuickInfo(snapshot, buffer.CurrentSnapshot, position)
		           ?? TryCreateLayerQuickInfo(snapshot, buffer.CurrentSnapshot, position);
		ArchitectureVisualStudioLog.Info(item is null
			? "QuickInfo found no AnaalIJzer item at position " + position + "."
			: "QuickInfo created AnaalIJzer item at position " + position + ".");

		return item;
	}

	private static QuickInfoItem? TryCreateSiteQuickInfo(ArchitectureEditorSnapshot editorSnapshot, ITextSnapshot textSnapshot, int position)
	{
		var options = ArchitectureVisualStudioOptions.Current;
		foreach (var indicator in editorSnapshot.SiteIndicators.OrderBy(indicator => indicator.Span.Length))
		{
			if (!CanShowSiteQuickInfo(options, indicator))
			{
				continue;
			}

			if (!ContainsPosition(indicator.Span, position) || !TryCreateTrackingSpan(textSnapshot, indicator.Span, out var trackingSpan))
			{
				continue;
			}

			var content = ArchitectureQuickInfoContentBuilder.CreateSiteContent(indicator);
			var result = new QuickInfoItem(trackingSpan, content.ToString());

			return result;
		}

		return null;
	}

	private static bool CanShowSiteQuickInfo(ArchitectureEditorOptions options, ArchitectureDependencySiteIndicator indicator)
	{
		var result = options.IsSiteDiagnosticEnabled(indicator.Site)
		             || (options.IsSiteLayerInformationEnabled(indicator.Site)
		                 && !string.IsNullOrWhiteSpace(indicator.DependencyLayerPath));

		return result;
	}

	private static QuickInfoItem? TryCreateLayerQuickInfo(ArchitectureEditorSnapshot editorSnapshot, ITextSnapshot textSnapshot, int position)
	{
		foreach (var indicator in EnumerateLayerQuickInfoIndicators(editorSnapshot).OrderBy(indicator => indicator.IdentifierSpan.Length))
		{
			if (!ContainsPosition(indicator.IdentifierSpan, position) && !ContainsPosition(indicator.DeclarationSpan, position))
			{
				continue;
			}

			if (!TryCreateTrackingSpan(textSnapshot, indicator.IdentifierSpan, out var trackingSpan))
			{
				continue;
			}

			var content = ArchitectureQuickInfoContentBuilder.CreateLayerContent(indicator, ArchitectureVisualStudioOptions.Current);
			var result = new QuickInfoItem(trackingSpan, content.ToString());

			return result;
		}

		return null;
	}

	private static IEnumerable<ArchitectureLayerIndicator> EnumerateLayerQuickInfoIndicators(ArchitectureEditorSnapshot editorSnapshot)
	{
		foreach (var indicator in editorSnapshot.LayerIndicators)
		{
			yield return indicator;
		}

		if (!ArchitectureVisualStudioOptions.Current.ShowLayerBadgesWhenNotInLayer)
		{
			yield break;
		}

		foreach (var indicator in editorSnapshot.UnclassifiedTypeIndicators)
		{
			yield return indicator;
		}
	}

	private static bool ContainsPosition(Microsoft.CodeAnalysis.Text.TextSpan span, int position)
	{
		var result = position >= span.Start && position <= span.End;

		return result;
	}

	private static bool TryCreateTrackingSpan(ITextSnapshot snapshot, Microsoft.CodeAnalysis.Text.TextSpan sourceSpan, out ITrackingSpan trackingSpan)
	{
		if (sourceSpan.Start < 0 || sourceSpan.End > snapshot.Length)
		{
			trackingSpan = null!;
			return false;
		}

		var snapshotSpan = new SnapshotSpan(snapshot, sourceSpan.Start, Math.Max(0, sourceSpan.Length));
		trackingSpan = snapshot.CreateTrackingSpan(snapshotSpan, SpanTrackingMode.EdgeInclusive);
		return true;
	}
}
