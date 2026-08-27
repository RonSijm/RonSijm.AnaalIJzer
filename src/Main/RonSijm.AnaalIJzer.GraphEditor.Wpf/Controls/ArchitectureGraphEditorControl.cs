using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl : UserControl
{
	private readonly StackPanel _contentPanel = new();
	private readonly TextBlock _statusText = new() { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
	private readonly CheckBox _showCodeEvidence = new() { Content = "Show code evidence", IsChecked = true, Margin = new Thickness(8, 8, 8, 4), VerticalAlignment = VerticalAlignment.Center };
	private readonly Button _exportImageButton = new()
	{
		Content = "Export PNG",
		IsEnabled = false,
		Margin = new Thickness(8, 8, 0, 4),
		HorizontalAlignment = HorizontalAlignment.Right,
		MinWidth = 86
	};
	private readonly Border _inspectorPanel = new() { Margin = new Thickness(6, 4, 8, 8), Padding = new Thickness(8), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Background = Brushes.Transparent };
	private readonly ScrollViewer _inspectorScrollViewer;
	private readonly ArchitectureGraphEditorTheme _theme;
	private readonly ILogger? _logger;
	private readonly IArchitectureGraphEditService _editService;
	private readonly Action<string>? _infoLogger;
	private readonly Action<string>? _warningLogger;
	private readonly Func<ArchitectureGraphSnapshot, ArchitectureGraphSnapshot>? _snapshotReloader;
	private readonly Action<ArchitectureGraphSnapshot>? _snapshotPublisher;
	private readonly Func<string, bool> _confirmationHandler;
	private readonly Func<ArchitectureLayerCreationRequest?>? _layerCreationHandler;
	private ArchitectureGraphSelection _currentSelection = ArchitectureGraphSelection.None;
	private ArchitectureGraphSnapshot _snapshot;
	private ArchitectureGraphLayoutState _layoutState;
	private ArchitectureGraphFocusMode _focusMode;
	private readonly bool _useExportSizing;

	public bool HasExportableGraphs
	{
		get
		{
			var result = CanExportGraphs();

			return result;
		}
	}

	public ArchitectureGraphEditorControl(
		ArchitectureGraphSnapshot? snapshot = null,
		ArchitectureGraphFocusMode focusMode = ArchitectureGraphFocusMode.HighlightCurrent,
		ArchitectureGraphEditorTheme? theme = null,
		Action<string>? infoLogger = null,
		Action<string>? warningLogger = null,
		ILogger? logger = null,
		Func<ArchitectureGraphSnapshot, ArchitectureGraphSnapshot>? snapshotReloader = null,
		Action<ArchitectureGraphSnapshot>? snapshotPublisher = null,
		Func<string, bool>? confirmationHandler = null,
		bool useExportSizing = false)
		: this(
			snapshot,
			focusMode,
			theme,
			infoLogger,
			warningLogger,
			logger,
			editService: null,
			snapshotReloader,
			snapshotPublisher,
			confirmationHandler,
			layerCreationHandler: null,
			useExportSizing)
	{
	}

	internal ArchitectureGraphEditorControl(
		ArchitectureGraphSnapshot? snapshot,
		ArchitectureGraphFocusMode focusMode,
		ArchitectureGraphEditorTheme? theme,
		Action<string>? infoLogger,
		Action<string>? warningLogger,
		ILogger? logger,
		IArchitectureGraphEditService? editService,
		Func<ArchitectureGraphSnapshot, ArchitectureGraphSnapshot>? snapshotReloader,
		Action<ArchitectureGraphSnapshot>? snapshotPublisher,
		Func<string, bool>? confirmationHandler,
		Func<ArchitectureLayerCreationRequest?>? layerCreationHandler,
		bool useExportSizing = false)
	{
		this._snapshot = snapshot ?? ArchitectureGraphSnapshot.Empty;
		this._focusMode = focusMode;
		this._theme = theme ?? ArchitectureGraphEditorTheme.Default;
		this._logger = logger;
		this._editService = editService ?? new ArchitectureGraphEditService();
		this._infoLogger = infoLogger;
		this._warningLogger = warningLogger;
		this._snapshotReloader = snapshotReloader;
		this._snapshotPublisher = snapshotPublisher;
		this._confirmationHandler = confirmationHandler ?? Confirm;
		this._layerCreationHandler = layerCreationHandler;
		this._useExportSizing = useExportSizing;
		_layoutState = ArchitectureGraphLayoutState.Load(this._snapshot.ConfigurationSource, warningLogger);
		logger?.LogDebug("Creating architecture graph editor control. Has configuration: {HasConfiguration}. Focus mode: {FocusMode}.", this._snapshot.HasConfiguration, this._focusMode);
		var editorRoot = CreateEditorRoot();
		_inspectorScrollViewer = editorRoot.InspectorScrollViewer;
		Content = editorRoot.Root;
		Unloaded += (_, _) => _layoutState.Save();
		RenderSelection(ArchitectureGraphSelection.None);
		Render();
	}

	public void UpdateSnapshot(ArchitectureGraphSnapshot nextSnapshot, ArchitectureGraphFocusMode nextFocusMode)
	{
		_logger?.LogInformation(
			"Updating architecture graph snapshot. Layers: {LayerCount}. Rules: {RuleCount}. Focus mode: {FocusMode}.",
			nextSnapshot.Layers.Length,
			nextSnapshot.Rules.Length,
			nextFocusMode);
		_layoutState.Save();
		_snapshot = nextSnapshot;
		_focusMode = nextFocusMode;
		EnsureLayoutState(_snapshot.ConfigurationSource);
		Render();
	}

	internal void Select(ArchitectureGraphSelection selection)
	{
		RenderSelection(selection);
	}
}
