using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Graphs;
using RonSijm.AnaalIJzer.VisualStudio.Options;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Tags;

internal sealed partial class ArchitectureTagger
{
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
				+ ", NameRules="
				+ result.NameRuleIndicators.Length
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
}
