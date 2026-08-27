using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;
using RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Editor.Snapshots;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.Tags;

internal sealed partial class ArchitectureTagger :
	ITagger<IntraTextAdornmentTag>,
	ITagger<InterLineAdornmentTag>,
	ITagger<TextMarkerTag>,
	ITagger<ArchitectureLayerGlyphTag>,
	IDisposable
{
	private readonly ITextView _view;
	private readonly ITextBuffer _buffer;
	private readonly ArchitectureSnapshotProvider _snapshotProvider;
	private CancellationTokenSource? _refreshCancellation;
	private ArchitectureEditorSnapshot _snapshot = ArchitectureEditorSnapshot.Empty;

	public ArchitectureTagger(ITextView view, ITextBuffer buffer, ArchitectureSnapshotProvider snapshotProvider)
	{
		this._view = view;
		this._buffer = buffer;
		this._snapshotProvider = snapshotProvider;
		buffer.Changed += BufferChanged;
		view.LayoutChanged += ViewLayoutChanged;
		view.GotAggregateFocus += ViewGotAggregateFocus;
		view.Closed += ViewClosed;
		ArchitectureVisualStudioOptions.Changed += OptionsChanged;
		ArchitectureVisualStudioLog.Info("ArchitectureTagger created for buffer content type '" + buffer.ContentType.TypeName + "'.");
		QueueRefresh(TimeSpan.Zero);
	}

	public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

	public void Dispose()
	{
		_refreshCancellation?.Cancel();
		_buffer.Changed -= BufferChanged;
		_view.LayoutChanged -= ViewLayoutChanged;
		_view.GotAggregateFocus -= ViewGotAggregateFocus;
		_view.Closed -= ViewClosed;
		ArchitectureVisualStudioOptions.Changed -= OptionsChanged;
	}
}
