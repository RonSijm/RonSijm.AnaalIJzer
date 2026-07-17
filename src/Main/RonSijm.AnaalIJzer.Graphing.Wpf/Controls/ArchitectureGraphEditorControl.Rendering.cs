using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.Graphing.Building;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	public void Render()
	{
		try
		{
			RenderCore();
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to render architecture graph editor.");
			throw;
		}
	}

	private void RenderCore()
	{
		logger?.LogDebug(
			"Rendering architecture graph editor. Has configuration: {HasConfiguration}. Has issues: {HasIssues}. Layers: {LayerCount}. Rules: {RuleCount}.",
			snapshot.HasConfiguration,
			snapshot.HasConfigurationIssues,
			snapshot.Layers.Length,
			snapshot.Rules.Length);
		EnsureLayoutState(snapshot.ConfigurationSource);
		contentPanel.Children.Clear();
		exportImageButton.IsEnabled = false;
		showCodeEvidence.IsEnabled = snapshot.Evidence.HasEvidence;
		statusText.Foreground = theme.HintForeground;
		if (!snapshot.HasConfiguration)
		{
			statusText.Text = "Open a C# file in a project with AnaalIJzer settings to view its dependency graphs.";
			RenderSelection(ArchitectureGraphSelection.None);
			return;
		}

		if (snapshot.HasConfigurationIssues)
		{
			statusText.Text = "AnaalIJzer configuration has issues. Fix ARCH006 diagnostics before graph rendering.";
			RenderSelection(ArchitectureGraphSelection.None);
			return;
		}

		var evidenceText = snapshot.Evidence.HasEvidence
			? ". Code evidence: " + snapshot.Evidence.Types.Length + " types, " + snapshot.Evidence.Dependencies.Count(dependency => dependency.IsViolation) + " violation observations"
			: ". Code evidence is not loaded";
		statusText.Text = "Focus mode: " + focusMode + ". Current layers: " + FormatActiveLayers(snapshot) + evidenceText + ".";
		var groups = ArchitectureGraphViewModelBuilder.Build(snapshot, focusMode, showCodeEvidence.IsChecked == true && snapshot.Evidence.HasEvidence);
		logger?.LogInformation("Architecture graph rendered as {GroupCount} group(s).", groups.Length);
		if (groups.Length == 0)
		{
			contentPanel.Children.Add(CreateHintTextBlock("The configuration loaded, but no dependency graph groups were found.", new Thickness(8)));
			return;
		}

		foreach (var group in groups)
		{
			contentPanel.Children.Add(CreateGroup(group));
		}

		exportImageButton.IsEnabled = CanExportGraphs();
	}

	private UIElement CreateGroup(ArchitectureGraphGroupViewModel group)
	{
		const double defaultGraphHeight = 460;
		const double minimumGraphHeight = 260;

		var groupKey = CreateGroupKey(group);
		var border = new Border
		{
			BorderThickness = new Thickness(group.IsHighlighted ? 2 : 1),
			Margin = new Thickness(8, 4, 8, 8),
			Padding = new Thickness(6)
		};
		theme.ApplyBackground(border);
		theme.ApplyBorder(border, group.IsHighlighted);

		var panel = new StackPanel();
		if (group.Nodes.Length > 0)
		{
			var graphHeight = useExportSizing
				? CalculateExportGraphHeight(group, minimumGraphHeight)
				: Math.Max(minimumGraphHeight, layoutState.GetGroupHeight(groupKey, defaultGraphHeight));
			var canvas = new ArchitectureGraphCanvas(
				group,
				(result, clearSelection) => HandleEditResult(result, clearSelection),
				RenderSelection,
				confirmationHandler,
				theme.CanvasTheme,
				logger,
				layerCreationHandler,
				layoutState,
				useExportSizing)
			{
				Height = graphHeight,
				MinHeight = minimumGraphHeight,
				MinWidth = 520,
				Margin = new Thickness(0, 6, 0, 4)
			};
			panel.Children.Add(canvas);
			panel.Children.Add(CreateGroupResizeThumb(groupKey, canvas));
		}

		if (group.Rules.Length > 0)
		{
			var expander = new Expander
			{
				Header = "Rule details",
				IsExpanded = group.Nodes.Length == 0,
				Margin = new Thickness(0, 6, 0, 0)
			};
			var details = new StackPanel();
			AddSection(details, "Layers", group.Layers);
			AddSection(details, "Rules", group.Rules);
			expander.Content = details;
			panel.Children.Add(expander);
		}

		var groupExpander = new Expander
		{
			Header = group.Title,
			IsExpanded = !layoutState.GetGroupIsCollapsed(groupKey, false),
			Foreground = theme.Foreground,
			Content = panel
		};
		groupExpander.Expanded += (_, _) =>
		{
			layoutState.SetGroupIsCollapsed(groupKey, false);
			layoutState.Save();
		};
		groupExpander.Collapsed += (_, _) =>
		{
			layoutState.SetGroupIsCollapsed(groupKey, true);
			layoutState.Save();
		};
		border.Child = groupExpander;

		return border;
	}

	private static double CalculateExportGraphHeight(ArchitectureGraphGroupViewModel group, double minimumGraphHeight)
	{
		const double nodeHeight = 96;
		const double padding = 120;
		var maxNodeY = group.Nodes.Length == 0
			? 0
			: group.Nodes.Max(node => node.Y + nodeHeight);
		var maxBoundaryY = group.Boundaries.Length == 0
			? 0
			: group.Boundaries.Max(boundary => boundary.Y + boundary.Height + padding);
		var result = Math.Max(minimumGraphHeight, Math.Max(maxNodeY, maxBoundaryY) + padding);

		return result;
	}

	private Thumb CreateGroupResizeThumb(string groupKey, FrameworkElement canvas)
	{
		var thumb = new Thumb
		{
			Height = 9,
			Cursor = Cursors.SizeNS,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Margin = new Thickness(0, 0, 0, 4),
			Background = theme.Border,
			Opacity = 0.42,
			ToolTip = "Drag to resize this graph."
		};
		thumb.DragDelta += (_, args) =>
		{
			var currentHeight = double.IsNaN(canvas.Height) || canvas.Height <= 0 ? canvas.ActualHeight : canvas.Height;
			var nextHeight = Math.Max(canvas.MinHeight, currentHeight + args.VerticalChange);
			canvas.Height = nextHeight;
			layoutState.SetGroupHeight(groupKey, nextHeight);
			args.Handled = true;
		};
		thumb.DragCompleted += (_, _) => layoutState.Save();

		return thumb;
	}
}
