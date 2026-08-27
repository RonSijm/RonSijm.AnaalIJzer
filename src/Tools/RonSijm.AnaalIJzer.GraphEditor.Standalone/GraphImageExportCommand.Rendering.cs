using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RonSijm.AnaalIJzer.Graphing.Building;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;
using RonSijm.AnaalIJzer.Graphing.Wpf.Exporting;
using RonSijm.AnaalIJzer.GraphModel.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Standalone;

internal sealed partial class GraphImageExportCommand
{
	public void ExportGraph(ArchitectureGraphSnapshot snapshot, string outputPath)
	{
		var control = CreateControl(snapshot);
		if (!control.HasExportableGraphs)
		{
			throw new InvalidOperationException(CreateNoGraphMessage(snapshot));
		}

		control.ExportGraphsAsPng(outputPath);
	}

	public void ExportPlaceholder(string outputPath, string title, string message)
	{
		var stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = "No graph image was generated for " + title,
			FontSize = 18,
			FontWeight = FontWeights.SemiBold,
			Foreground = SystemColors.ControlTextBrush,
			TextWrapping = TextWrapping.Wrap
		});
		stack.Children.Add(new TextBlock
		{
			Text = message,
			Margin = new Thickness(0, 12, 0, 0),
			Foreground = SystemColors.ControlTextBrush,
			TextWrapping = TextWrapping.Wrap
		});
		var border = new Border
		{
			Width = _width,
			Height = Math.Min(_height, 420),
			Padding = new Thickness(24),
			Background = SystemColors.WindowBrush,
			BorderBrush = SystemColors.ActiveBorderBrush,
			BorderThickness = new Thickness(1),
			Child = stack
		};
		var size = new Size(_width, Math.Min(_height, 420));
		border.Measure(size);
		border.Arrange(new Rect(size));
		border.UpdateLayout();
		ArchitectureGraphImageExporter.SavePng(border, outputPath, SystemColors.WindowBrush);
	}

	private ArchitectureGraphEditorControl CreateControl(ArchitectureGraphSnapshot snapshot)
	{
		var control = new ArchitectureGraphEditorControl(snapshot, ArchitectureGraphFocusMode.ShowAll, logger: null, useExportSizing: true);
		var size = CalculateExportSize(snapshot);
		control.Measure(size);
		var arrangedSize = new Size(size.Width, Math.Max(size.Height, control.DesiredSize.Height));
		control.Arrange(new Rect(arrangedSize));
		control.UpdateLayout();
		DrainDispatcher();

		return control;
	}

	private Size CalculateExportSize(ArchitectureGraphSnapshot snapshot)
	{
		var groups = ArchitectureGraphViewModelBuilder.Build(
			snapshot,
			ArchitectureGraphFocusMode.ShowAll,
			snapshot.Evidence.HasEvidence);
		if (groups.Length == 0)
		{
			var fallbackSize = new Size(_width, _height);

			return fallbackSize;
		}

		var contentWidth = groups.Max(CalculateGroupExportWidth);
		var contentHeight = groups.Sum(CalculateGroupExportHeight);
		var exportWidth = Math.Min(_width, Math.Max(ExportMinimumWidth, contentWidth));
		var exportHeight = Math.Max(ExportMinimumHeight, contentHeight);
		var result = new Size(Math.Ceiling(exportWidth), Math.Ceiling(exportHeight));

		return result;
	}

	private static double CalculateGroupExportWidth(ArchitectureGraphGroupViewModel group)
	{
		var maxNodeX = group.Nodes.Length == 0
			? 0
			: group.Nodes.Max(node => node.X + ExportNodeWidth);
		var maxBoundaryX = group.Boundaries.Length == 0
			? 0
			: group.Boundaries.Max(boundary => boundary.X + boundary.Width);
		var result = Math.Max(maxNodeX, maxBoundaryX) + ExportPadding;

		return result;
	}

	private static double CalculateGroupExportHeight(ArchitectureGraphGroupViewModel group)
	{
		if (group.Nodes.Length == 0)
		{
			return ExportMinimumHeight;
		}

		var maxNodeY = group.Nodes.Max(node => node.Y + ExportNodeHeight);
		var maxBoundaryY = group.Boundaries.Length == 0
			? 0
			: group.Boundaries.Max(boundary => boundary.Y + boundary.Height);
		var result = Math.Max(ExportMinimumHeight, Math.Max(maxNodeY, maxBoundaryY) + ExportPadding + ExportGroupChromeHeight);

		return result;
	}

	private static string CreateNoGraphMessage(ArchitectureGraphSnapshot snapshot)
	{
		var result = snapshot.ConfigurationIssueMessages.Length > 0
			? string.Join(Environment.NewLine, snapshot.ConfigurationIssueMessages)
			: "No renderable dependency graph was found.";

		return result;
	}

	private static void DrainDispatcher()
	{
		var frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
		Dispatcher.PushFrame(frame);
	}
}
