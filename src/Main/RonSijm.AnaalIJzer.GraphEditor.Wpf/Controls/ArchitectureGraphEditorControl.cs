using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl : UserControl
{
	private readonly StackPanel contentPanel = new();
	private readonly TextBlock statusText = new() { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
	private readonly CheckBox showCodeEvidence = new() { Content = "Show code evidence", IsChecked = true, Margin = new Thickness(8, 8, 8, 4), VerticalAlignment = VerticalAlignment.Center };
	private readonly Button exportImageButton = new()
	{
		Content = "Export PNG",
		IsEnabled = false,
		Margin = new Thickness(8, 8, 0, 4),
		HorizontalAlignment = HorizontalAlignment.Right,
		MinWidth = 86
	};
	private readonly Border inspectorPanel = new() { Margin = new Thickness(6, 4, 8, 8), Padding = new Thickness(8), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Background = Brushes.Transparent };
	private readonly ScrollViewer inspectorScrollViewer;
	private readonly ArchitectureGraphEditorTheme theme;
	private readonly ILogger? logger;
	private readonly IArchitectureGraphEditService editService;
	private readonly Action<string>? infoLogger;
	private readonly Action<string>? warningLogger;
	private readonly Func<ArchitectureGraphSnapshot, ArchitectureGraphSnapshot>? snapshotReloader;
	private readonly Action<ArchitectureGraphSnapshot>? snapshotPublisher;
	private readonly Func<string, bool> confirmationHandler;
	private readonly Func<ArchitectureLayerCreationRequest?>? layerCreationHandler;
	private ArchitectureGraphSelection currentSelection = ArchitectureGraphSelection.None;
	private ArchitectureGraphSnapshot snapshot;
	private ArchitectureGraphLayoutState layoutState;
	private ArchitectureGraphFocusMode focusMode;
	private readonly bool useExportSizing;

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
		this.snapshot = snapshot ?? ArchitectureGraphSnapshot.Empty;
		this.focusMode = focusMode;
		this.theme = theme ?? ArchitectureGraphEditorTheme.Default;
		this.logger = logger;
		this.editService = editService ?? new ArchitectureGraphEditService();
		this.infoLogger = infoLogger;
		this.warningLogger = warningLogger;
		this.snapshotReloader = snapshotReloader;
		this.snapshotPublisher = snapshotPublisher;
		this.confirmationHandler = confirmationHandler ?? Confirm;
		this.layerCreationHandler = layerCreationHandler;
		this.useExportSizing = useExportSizing;
		layoutState = ArchitectureGraphLayoutState.Load(this.snapshot.ConfigurationSource, warningLogger);
		logger?.LogDebug("Creating architecture graph editor control. Has configuration: {HasConfiguration}. Focus mode: {FocusMode}.", this.snapshot.HasConfiguration, this.focusMode);
		var editorRoot = CreateEditorRoot();
		inspectorScrollViewer = editorRoot.InspectorScrollViewer;
		Content = editorRoot.Root;
		Unloaded += (_, _) => layoutState.Save();
		RenderSelection(ArchitectureGraphSelection.None);
		Render();
	}

	public void UpdateSnapshot(ArchitectureGraphSnapshot nextSnapshot, ArchitectureGraphFocusMode nextFocusMode)
	{
		logger?.LogInformation(
			"Updating architecture graph snapshot. Layers: {LayerCount}. Rules: {RuleCount}. Focus mode: {FocusMode}.",
			nextSnapshot.Layers.Length,
			nextSnapshot.Rules.Length,
			nextFocusMode);
		layoutState.Save();
		snapshot = nextSnapshot;
		focusMode = nextFocusMode;
		EnsureLayoutState(snapshot.ConfigurationSource);
		Render();
	}

	internal void Select(ArchitectureGraphSelection selection)
	{
		RenderSelection(selection);
	}
}
