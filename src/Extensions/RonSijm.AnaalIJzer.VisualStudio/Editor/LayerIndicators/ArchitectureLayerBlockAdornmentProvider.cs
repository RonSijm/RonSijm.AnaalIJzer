using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("CSharp")]
[TextViewRole(PredefinedTextViewRoles.Document)]
[method: ImportingConstructor]
internal sealed class ArchitectureLayerBlockAdornmentProvider(ArchitectureSnapshotProvider snapshotProvider)
    : IWpfTextViewCreationListener
{
	internal const string LayerName = "AnaalIJzer Layer Block Highlight";

    [Export(typeof(AdornmentLayerDefinition))]
	[Name(LayerName)]
	[Order(Before = PredefinedAdornmentLayers.Text)]
	internal AdornmentLayerDefinition? LayerDefinition { get; set; }

	public void TextViewCreated(IWpfTextView textView)
	{
		_ = new ArchitectureLayerBlockAdornmentManager(textView, snapshotProvider);
	}
}

internal sealed class ArchitectureLayerBlockAdornmentManager : IDisposable
{
	private readonly IWpfTextView _view;
	private readonly ArchitectureSnapshotProvider _snapshotProvider;
	private readonly IAdornmentLayer _adornmentLayer;
	private CancellationTokenSource? _refreshCancellation;
	private ArchitectureEditorSnapshot _snapshot = ArchitectureEditorSnapshot.Empty;

	public ArchitectureLayerBlockAdornmentManager(IWpfTextView view, ArchitectureSnapshotProvider snapshotProvider)
	{
		this._view = view;
		this._snapshotProvider = snapshotProvider;
		_adornmentLayer = view.GetAdornmentLayer(ArchitectureLayerBlockAdornmentProvider.LayerName);
		view.LayoutChanged += ViewLayoutChanged;
		view.Closed += ViewClosed;
		ArchitectureVisualStudioOptions.Changed += OptionsChanged;
		ArchitectureVisualStudioLog.Info("ArchitectureLayerBlockAdornmentManager created for content type '" + view.TextBuffer.ContentType.TypeName + "'.");
		QueueRefresh(TimeSpan.Zero);
	}

	public void Dispose()
	{
		_refreshCancellation?.Cancel();
		_view.LayoutChanged -= ViewLayoutChanged;
		_view.Closed -= ViewClosed;
		ArchitectureVisualStudioOptions.Changed -= OptionsChanged;
		_adornmentLayer.RemoveAllAdornments();
	}

	private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
	{
		if (e.NewSnapshot != e.OldSnapshot)
		{
			QueueRefresh(TimeSpan.FromMilliseconds(250));
			return;
		}

		Redraw();
	}

	private void ViewClosed(object sender, EventArgs e)
	{
		Dispose();
	}

	private void OptionsChanged(object sender, EventArgs e)
	{
		Redraw();
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

			var result = await _snapshotProvider.CreateSnapshotAsync(_view.TextBuffer, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			_snapshot = result;
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			if (!cancellationToken.IsCancellationRequested)
			{
				Redraw();
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			ArchitectureVisualStudioLog.Exception("Layer block adornment refresh failed.", exception);
		}
	}

	private void Redraw()
	{
		_adornmentLayer.RemoveAllAdornments();
		var options = ArchitectureVisualStudioOptions.Current;
		if (!options.EnableLayerBlockHighlight || !_snapshot.HasConfiguration || _snapshot.HasConfigurationIssues)
		{
			return;
		}

		foreach (var indicator in _snapshot.LayerIndicators)
		{
			if (!indicator.IsInLayer || !TryCreateSnapshotSpan(_view.TextSnapshot, indicator.DeclarationSpan, out var span))
			{
				continue;
			}

			AddAdornment(indicator, span);
		}
	}

	private void AddAdornment(ArchitectureLayerIndicator indicator, SnapshotSpan span)
	{
		var textViewLines = _view.TextViewLines.GetTextViewLinesIntersectingSpan(span).ToArray();
		if (textViewLines.Length == 0)
		{
			return;
		}

		var color = ArchitecturePalette.GetBrush(indicator.PaletteSlot).Color;
		var top = textViewLines.Min(line => line.Top);
		var bottom = textViewLines.Max(line => line.Bottom);
		var height = Math.Max(1, bottom - top);
		var width = Math.Max(1, _view.ViewportWidth);
		var border = new Border
		{
			Width = width,
			Height = height,
			Background = CreateBrush(color, 14),
			BorderBrush = CreateBrush(color, 92),
			BorderThickness = new Thickness(1),
			IsHitTestVisible = false,
			SnapsToDevicePixels = true
		};

		Canvas.SetLeft(border, _view.ViewportLeft);
		Canvas.SetTop(border, top);
		_adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, indicator, border, null);
	}

	private static SolidColorBrush CreateBrush(Color color, byte alpha)
	{
		var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
		brush.Freeze();

		return brush;
	}

	private static bool TryCreateSnapshotSpan(ITextSnapshot textSnapshot, Microsoft.CodeAnalysis.Text.TextSpan sourceSpan, out SnapshotSpan span)
	{
		if (sourceSpan.Start < 0 || sourceSpan.End > textSnapshot.Length)
		{
			span = default;
			return false;
		}

		span = new SnapshotSpan(textSnapshot, sourceSpan.Start, sourceSpan.Length);
		return true;
	}
}
