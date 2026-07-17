using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf.Exporting;
using RonSijm.AnaalIJzer.Graphing.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

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
	private readonly Action<string>? infoLogger;
	private readonly Action<string>? warningLogger;
	private readonly Func<ArchitectureGraphSnapshot, ArchitectureGraphSnapshot>? snapshotReloader;
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
		Func<string, bool>? confirmationHandler = null,
		Func<ArchitectureLayerCreationRequest?>? layerCreationHandler = null,
		bool useExportSizing = false)
	{
		this.snapshot = snapshot ?? ArchitectureGraphSnapshot.Empty;
		this.focusMode = focusMode;
		this.theme = theme ?? ArchitectureGraphEditorTheme.Default;
		this.logger = logger;
		this.infoLogger = infoLogger;
		this.warningLogger = warningLogger;
		this.snapshotReloader = snapshotReloader;
		this.confirmationHandler = confirmationHandler ?? Confirm;
		this.layerCreationHandler = layerCreationHandler;
		this.useExportSizing = useExportSizing;
		layoutState = ArchitectureGraphLayoutState.Load(this.snapshot.ConfigurationSource, warningLogger);
		logger?.LogDebug("Creating architecture graph editor control. Has configuration: {HasConfiguration}. Focus mode: {FocusMode}.", this.snapshot.HasConfiguration, this.focusMode);
		var root = new DockPanel();
		this.theme.ApplyToRoot(root);
		this.theme.ApplyBackground(root);
		this.theme.ApplyBackground(inspectorPanel);
		var header = new DockPanel();
		var heading = new TextBlock
		{
			Text = "AnaalIJzer Dependency Graphs",
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(8, 8, 8, 4)
		};
		var refresh = new Button
		{
			Content = "Refresh",
			Margin = new Thickness(8, 8, 8, 4),
			HorizontalAlignment = HorizontalAlignment.Right,
			MinWidth = 72
		};
		refresh.Click += (_, _) =>
		{
			TryReloadSnapshot();
			Render();
			RenderSelection(RemapSelection(currentSelection));
		};
		exportImageButton.Click += (_, _) => PromptExportGraphsAsPng();
		showCodeEvidence.Checked += (_, _) => Render();
		showCodeEvidence.Unchecked += (_, _) => Render();
		DockPanel.SetDock(refresh, Dock.Right);
		header.Children.Add(refresh);
		DockPanel.SetDock(exportImageButton, Dock.Right);
		header.Children.Add(exportImageButton);
		DockPanel.SetDock(showCodeEvidence, Dock.Right);
		header.Children.Add(showCodeEvidence);
		header.Children.Add(heading);
		DockPanel.SetDock(header, Dock.Top);
		root.Children.Add(header);
		DockPanel.SetDock(statusText, Dock.Top);
		root.Children.Add(statusText);
		var editorGrid = new Grid();
		editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 360 });
		editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
		editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380), MinWidth = 300 });
		var graphScroll = new ScrollViewer { Content = contentPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		Grid.SetColumn(graphScroll, 0);
		editorGrid.Children.Add(graphScroll);
		var splitter = new GridSplitter
		{
			Width = 5,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = inspectorPanel.BorderBrush,
			ResizeBehavior = GridResizeBehavior.PreviousAndNext
		};
		Grid.SetColumn(splitter, 1);
		editorGrid.Children.Add(splitter);
		inspectorScrollViewer = new ScrollViewer
		{
			Content = inspectorPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		Grid.SetColumn(inspectorScrollViewer, 2);
		editorGrid.Children.Add(inspectorScrollViewer);
		root.Children.Add(editorGrid);
		Content = root;
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

	public void Select(ArchitectureGraphSelection selection)
	{
		RenderSelection(selection);
	}

	public void ExportGraphsAsPng(string path)
	{
		if (!CanExportGraphs())
		{
			throw new InvalidOperationException("There are no rendered dependency graphs to export.");
		}

		ArchitectureGraphImageExporter.SavePng(contentPanel, path, theme.Background);
		infoLogger?.Invoke("Exported dependency graph image to " + path + ".");
		logger?.LogInformation("Exported dependency graph image to {Path}", path);
	}

	private void PromptExportGraphsAsPng()
	{
		if (!CanExportGraphs())
		{
			return;
		}

		var dialog = new SaveFileDialog
		{
			Title = "Export AnaalIJzer dependency graphs",
			FileName = "architecture-dependency-graphs.png",
			DefaultExt = ".png",
			Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
			OverwritePrompt = true
		};
		if (dialog.ShowDialog() != true)
		{
			return;
		}

		try
		{
			ExportGraphsAsPng(dialog.FileName);
		}
		catch (Exception exception)
		{
			warningLogger?.Invoke(exception.Message);
			logger?.LogError(exception, "Failed to export dependency graph image to {Path}", dialog.FileName);
			MessageBox.Show(exception.Message, "AnaalIJzer Dependency Graphs", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	private bool CanExportGraphs()
	{
		var result = snapshot.HasConfiguration
		             && !snapshot.HasConfigurationIssues
		             && contentPanel.Children.Count > 0;

		return result;
	}

}
