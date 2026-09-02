using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Graphs;

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
		var context = _snapshotProvider.CreateGraphToolWindowContext(_buffer, _snapshot);
		ArchitectureGraphToolWindowState.Publish(context);
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
		_refreshCancellation?.Cancel();
		var cancellation = new CancellationTokenSource();
		_refreshCancellation = cancellation;
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

			var result = await _snapshotProvider.CreateSnapshotAsync(_buffer, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			_snapshot = result;
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
			if (_view.HasAggregateFocus)
			{
				var context = _snapshotProvider.CreateGraphToolWindowContext(_buffer, _snapshot);
				ArchitectureGraphToolWindowState.Publish(context);
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
		var currentSnapshot = _buffer.CurrentSnapshot;
		var span = new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length);
		TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
	}
}
