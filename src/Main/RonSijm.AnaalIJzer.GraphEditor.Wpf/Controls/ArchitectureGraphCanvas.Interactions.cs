using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private void ConnectionLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.ContextMenu is null)
		{
			element.ContextMenu = CreateConnectionContextMenu();
		}
	}

	private void NodeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (sender is FrameworkElement { DataContext: NodifyGraphNodeViewModel node })
			{
				logger?.LogDebug("Selected layer node '{LayerPath}'.", node.Path);
				selectionHandler?.Invoke(ArchitectureGraphSelection.ForLayer(node.EditHandle));
			}
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to select layer node.");
			ReportEditResult(ArchitectureConfigurationEditResult.Failure("Selecting the layer failed. See the graph editor log for details."));
			e.Handled = true;
		}
	}

	private void BoundaryMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (sender is FrameworkElement { DataContext: NodifyGraphBoundaryViewModel boundary })
			{
				logger?.LogDebug("Selected layer boundary '{LayerPath}'.", boundary.Path);
				selectionHandler?.Invoke(ArchitectureGraphSelection.ForLayer(boundary.EditHandle));
			}
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to select layer boundary.");
			ReportEditResult(ArchitectureConfigurationEditResult.Failure("Selecting the layer failed. See the graph editor log for details."));
			e.Handled = true;
		}
	}

	private void ConnectionMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (sender is FrameworkElement { DataContext: NodifyGraphConnectionViewModel connection })
			{
				logger?.LogDebug("Selected dependency connection '{Kind}' from '{From}' to '{To}'.", connection.Kind, connection.From, connection.To);
				selectionHandler?.Invoke(connection.IsEvidence
					? ArchitectureGraphSelection.ForCodeEvidence(connection.From, connection.To, connection.LabelText, connection.EvidenceDetails)
					: ArchitectureGraphSelection.ForDependency(connection.EditHandle));
				e.Handled = true;
			}
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to select dependency connection.");
			ReportEditResult(ArchitectureConfigurationEditResult.Failure("Selecting the dependency failed. See the graph editor log for details."));
			e.Handled = true;
		}
	}

	private ArchitectureLayerCreationRequest? PromptForLayerCreation()
	{
		var result = ArchitectureLayerCreationDialog.Prompt(Window.GetWindow(this), theme);

		return result;
	}

	private void ReportEditResult(ArchitectureConfigurationEditResult result)
	{
		if (result.Succeeded)
		{
			logger?.LogInformation("Graph edit succeeded: {Message}", result.Message);
		}
		else
		{
			logger?.LogWarning("Graph edit failed: {Message}", result.Message);
		}

		editResultHandler?.Invoke(result, false);
	}
}
