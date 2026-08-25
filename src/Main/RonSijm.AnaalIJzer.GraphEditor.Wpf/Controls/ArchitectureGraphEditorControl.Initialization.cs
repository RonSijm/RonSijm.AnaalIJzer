using System.Windows;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private (DockPanel Root, ScrollViewer InspectorScrollViewer) CreateEditorRoot()
	{
		var root = new DockPanel();
		theme.ApplyToRoot(root);
		theme.ApplyBackground(root);
		theme.ApplyBackground(inspectorPanel);
		DockPanel.SetDock(statusText, Dock.Top);
		root.Children.Add(CreateHeader());
		root.Children.Add(statusText);
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
			Content = contentPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
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

		var createdInspectorScrollViewer = new ScrollViewer
		{
			Content = inspectorPanel,
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
		RenderSelection(RemapSelection(currentSelection));
	}
}
