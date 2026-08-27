using System.Windows;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private (DockPanel Root, ScrollViewer InspectorScrollViewer) CreateEditorRoot()
	{
		var root = new DockPanel();
		_theme.ApplyToRoot(root);
		_theme.ApplyBackground(root);
		_theme.ApplyBackground(_inspectorPanel);
		DockPanel.SetDock(_statusText, Dock.Top);
		root.Children.Add(CreateHeader());
		root.Children.Add(_statusText);
		var editorGrid = CreateEditorGrid();
		root.Children.Add(editorGrid.Grid);

		var result = (root, editorGrid.InspectorScrollViewer);

		return result;
	}

	private UIElement CreateHeader()
	{
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

		refresh.Click += (_, _) => RefreshCurrentView();
		_exportImageButton.Click += (_, _) => PromptExportGraphsAsPng();
		_showCodeEvidence.Checked += (_, _) => Render();
		_showCodeEvidence.Unchecked += (_, _) => Render();

		DockPanel.SetDock(refresh, Dock.Right);
		header.Children.Add(refresh);
		DockPanel.SetDock(_exportImageButton, Dock.Right);
		header.Children.Add(_exportImageButton);
		DockPanel.SetDock(_showCodeEvidence, Dock.Right);
		header.Children.Add(_showCodeEvidence);
		header.Children.Add(heading);
		DockPanel.SetDock(header, Dock.Top);

		return header;
	}

	private (UIElement Grid, ScrollViewer InspectorScrollViewer) CreateEditorGrid()
	{
		var editorGrid = new Grid();
		editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 360 });
		editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
		editorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380), MinWidth = 300 });

		var graphScroll = new ScrollViewer
		{
			Content = _contentPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		Grid.SetColumn(graphScroll, 0);
		editorGrid.Children.Add(graphScroll);

		var splitter = new GridSplitter
		{
			Width = 5,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = _inspectorPanel.BorderBrush,
			ResizeBehavior = GridResizeBehavior.PreviousAndNext
		};
		Grid.SetColumn(splitter, 1);
		editorGrid.Children.Add(splitter);

		var createdInspectorScrollViewer = new ScrollViewer
		{
			Content = _inspectorPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		Grid.SetColumn(createdInspectorScrollViewer, 2);
		editorGrid.Children.Add(createdInspectorScrollViewer);

		var result = ((UIElement)editorGrid, createdInspectorScrollViewer);

		return result;
	}

	private void RefreshCurrentView()
	{
		TryReloadSnapshot();
		Render();
		RenderSelection(RemapSelection(_currentSelection));
	}
}
