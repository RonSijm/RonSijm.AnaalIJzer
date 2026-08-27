using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.GraphModel.Loading;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private UIElement CreateNoConfigurationPanel()
	{
		var border = CreateActionPanel();
		var panel = new StackPanel();
		panel.Children.Add(CreateSectionTitle("Create architecture settings"));
		panel.Children.Add(CreateHintTextBlock("No AnaalIJzer settings were found for the current context. Create an Architecture.anl file first, then add layers and dependency rules from the graph editor.", new Thickness(0, 2, 0, 8)));
		if (_snapshot.ConfigurationCreationTargets.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("No project, source folder, or solution folder was available for creating Architecture.anl.", new Thickness(0, 0, 0, 0)));
			border.Child = panel;

			return border;
		}

		foreach (var target in _snapshot.ConfigurationCreationTargets)
		{
			panel.Children.Add(CreateConfigurationCreationTargetButton(target));
		}

		border.Child = panel;

		return border;
	}

	private UIElement CreateEmptyConfigurationPanel()
	{
		var border = CreateActionPanel();
		var panel = new StackPanel();
		panel.Children.Add(CreateSectionTitle("Start the graph"));
		panel.Children.Add(CreateHintTextBlock("This configuration is valid, but it has no layers yet. Add the first layer to turn it into an editable dependency graph.", new Thickness(0, 2, 0, 8)));
		AddReadOnlyRow(panel, "Source", _snapshot.ConfigurationSource.CanEdit ? _snapshot.ConfigurationSource.Path : "Not editable");
		var addLayer = new Button
		{
			Content = "Add first layer",
			IsEnabled = _snapshot.ConfigurationSource.CanEdit,
			MinWidth = 120,
			Margin = new Thickness(0, 10, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		addLayer.Click += (_, _) => PromptAddRootLayer();
		panel.Children.Add(addLayer);
		border.Child = panel;

		return border;
	}

	private Border CreateActionPanel()
	{
		var result = new Border
		{
			BorderThickness = new Thickness(1),
			Margin = new Thickness(8),
			Padding = new Thickness(12),
			MaxWidth = 760,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_theme.ApplyBackground(result);
		_theme.ApplyBorder(result, false);

		return result;
	}

	private Button CreateConfigurationCreationTargetButton(ArchitectureConfigurationCreationTarget target)
	{
		var content = new StackPanel();
		content.Children.Add(new TextBlock { Text = "Create in " + target.Title, FontWeight = FontWeights.SemiBold });
		content.Children.Add(CreateHintTextBlock(target.Description, new Thickness(0, 2, 0, 0)));
		content.Children.Add(new TextBlock { Text = target.Source.Path, TextWrapping = TextWrapping.Wrap, FontFamily = new System.Windows.Media.FontFamily("Consolas"), Margin = new Thickness(0, 4, 0, 0) });
		if (!string.IsNullOrWhiteSpace(target.RegistrationPath))
		{
			content.Children.Add(new TextBlock { Text = "MSBuild: " + target.RegistrationPath, TextWrapping = TextWrapping.Wrap, FontFamily = new System.Windows.Media.FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 0) });
		}

		var result = new Button
		{
			Content = content,
			Margin = new Thickness(0, 4, 0, 8),
			Padding = new Thickness(8),
			HorizontalContentAlignment = HorizontalAlignment.Stretch
		};
		result.Click += (_, _) => CreateConfiguration(target);

		return result;
	}

	private void CreateConfiguration(ArchitectureConfigurationCreationTarget target)
	{
		try
		{
			var result = _editService.CreateConfiguration(target);
			if (!result.Succeeded)
			{
				HandleEditResult(result, true, false);
				return;
			}

			var reloaded = ArchitectureGraphXmlSnapshotLoader.Load(target.Source);
			_snapshot = reloaded;
			_snapshotPublisher?.Invoke(reloaded);
			EnsureLayoutState(_snapshot.ConfigurationSource);
			Render();
			RenderSelection(ArchitectureGraphSelection.None);
			_statusText.Text = result.Message;
			_statusText.Foreground = _theme.SuccessForeground;
			_infoLogger?.Invoke(result.Message);
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to create AnaalIJzer configuration at {Path}.", target.Source.Path);
			HandleEditResult(ArchitectureConfigurationEditResult.Failure(exception.Message), true, false);
		}
	}

	private void PromptAddRootLayer()
	{
		var request = _layerCreationHandler?.Invoke()
		              ?? ArchitectureLayerCreationDialog.Prompt(Window.GetWindow(this), _theme.CanvasTheme);
		if (request is null)
		{
			return;
		}

		var result = _editService.AddLayer(
			_snapshot.ConfigurationSource,
			string.Empty,
			request.Name,
			request.MatcherKind,
			request.MatcherAttributes);
		HandleEditResult(result, true);
	}
}
