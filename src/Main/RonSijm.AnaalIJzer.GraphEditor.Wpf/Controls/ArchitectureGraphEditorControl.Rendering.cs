using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.Graphing.Building;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

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
			_logger?.LogError(exception, "Failed to render architecture graph editor.");
			throw;
		}
	}

	private void RenderCore()
	{
		_logger?.LogDebug(
			"Rendering architecture graph editor. Has configuration: {HasConfiguration}. Has issues: {HasIssues}. Layers: {LayerCount}. Rules: {RuleCount}.",
			_snapshot.HasConfiguration,
			_snapshot.HasConfigurationIssues,
			_snapshot.Layers.Length,
			_snapshot.Rules.Length);
		EnsureLayoutState(_snapshot.ConfigurationSource);
		_contentPanel.Children.Clear();
		_exportImageButton.IsEnabled = false;
		_showCodeEvidence.IsEnabled = _snapshot.Evidence.HasEvidence;
		_statusText.Foreground = _theme.HintForeground;
		if (!_snapshot.HasConfiguration)
		{
			_statusText.Text = "No AnaalIJzer settings were found for the current context.";
			_contentPanel.Children.Add(CreateNoConfigurationPanel());
			RenderSelection(ArchitectureGraphSelection.None);
			return;
		}

		if (_snapshot.HasConfigurationIssues)
		{
			_statusText.Text = "AnaalIJzer configuration has issues. Fix ARCH006 diagnostics before graph rendering.";
			RenderSelection(ArchitectureGraphSelection.None);
			return;
		}

		var evidenceText = _snapshot.Evidence.HasEvidence
			? ". Code evidence: " + _snapshot.Evidence.Types.Length + " types, " + _snapshot.Evidence.Dependencies.Count(dependency => dependency.IsViolation) + " violation observations"
			: ". Code evidence is not loaded";
		_statusText.Text = "Focus mode: " + _focusMode + ". Current layers: " + FormatActiveLayers(_snapshot) + evidenceText + ".";
		var groups = ArchitectureGraphViewModelBuilder.Build(_snapshot, _focusMode, _showCodeEvidence.IsChecked == true && _snapshot.Evidence.HasEvidence);
		_logger?.LogInformation("Architecture graph rendered as {GroupCount} group(s).", groups.Length);
		if (groups.Length == 0)
		{
			_contentPanel.Children.Add(CreateEmptyConfigurationPanel());
			return;
		}

		foreach (var group in groups)
		{
			_contentPanel.Children.Add(CreateGroup(group));
		}

		_exportImageButton.IsEnabled = CanExportGraphs();
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
		_theme.ApplyBackground(border);
		_theme.ApplyBorder(border, group.IsHighlighted);

		var panel = new StackPanel();
		if (group.Nodes.Length > 0)
		{
			var graphHeight = _useExportSizing
				? CalculateExportGraphHeight(group, minimumGraphHeight)
				: Math.Max(minimumGraphHeight, _layoutState.GetGroupHeight(groupKey, defaultGraphHeight));
			var canvas = new ArchitectureGraphCanvas(
				group,
				(result, clearSelection) => HandleEditResult(result, clearSelection),
				RenderSelection,
				_confirmationHandler,
				_theme.CanvasTheme,
				_logger,
				_layerCreationHandler,
				_layoutState,
				_editService,
				_useExportSizing)
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
			IsExpanded = !_layoutState.GetGroupIsCollapsed(groupKey, false),
			Foreground = _theme.Foreground,
			Content = panel
		};
		groupExpander.Expanded += (_, _) =>
		{
			_layoutState.SetGroupIsCollapsed(groupKey, false);
			_layoutState.Save();
		};
		groupExpander.Collapsed += (_, _) =>
		{
			_layoutState.SetGroupIsCollapsed(groupKey, true);
			_layoutState.Save();
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
			Background = _theme.Border,
			Opacity = 0.42,
			ToolTip = "Drag to resize this graph."
		};
		thumb.DragDelta += (_, args) =>
		{
			var currentHeight = double.IsNaN(canvas.Height) || canvas.Height <= 0 ? canvas.ActualHeight : canvas.Height;
			var nextHeight = Math.Max(canvas.MinHeight, currentHeight + args.VerticalChange);
			canvas.Height = nextHeight;
			_layoutState.SetGroupHeight(groupKey, nextHeight);
			args.Handled = true;
		};
		thumb.DragCompleted += (_, _) => _layoutState.Save();

		return thumb;
	}
}
