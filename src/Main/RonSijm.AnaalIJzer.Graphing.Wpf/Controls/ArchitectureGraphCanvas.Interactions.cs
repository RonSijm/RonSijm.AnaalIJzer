using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphCanvas
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

	private ContextMenu CreateConnectionContextMenu()
	{
		var menu = new ContextMenu();
		theme.ApplyToContextMenu(menu);
		menu.Opened += ConnectionContextMenuOpened;
		var remove = new MenuItem { Header = "Remove connection" };
		remove.SetBinding(MenuItem.CommandProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.RemoveCommand)));
		remove.SetBinding(UIElement.IsEnabledProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.CanEditRule), BindingMode.OneWay));
		menu.Items.Add(remove);
		menu.Items.Add(new Separator());

		var allSites = new MenuItem { Header = "Allow all sites", IsCheckable = true };
		allSites.SetBinding(MenuItem.IsCheckedProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.UsesAllSites), BindingMode.OneWay));
		allSites.SetBinding(MenuItem.CommandProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.AllowAllSitesCommand)));
		allSites.SetBinding(UIElement.IsEnabledProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.CanEditRule), BindingMode.OneWay));
		menu.Items.Add(allSites);

		var allowedSites = new MenuItem { Header = "allowedSites" };
		allowedSites.SetBinding(ItemsControl.ItemsSourceProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.AllowedSiteOptions)));
		allowedSites.SetBinding(UIElement.IsEnabledProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.CanEditRule), BindingMode.OneWay));
		allowedSites.ItemContainerStyle = CreateSiteOptionStyle(theme);
		menu.Items.Add(allowedSites);

		var blockedSites = new MenuItem { Header = "blockedSites" };
		blockedSites.SetBinding(ItemsControl.ItemsSourceProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.BlockedSiteOptions)));
		blockedSites.SetBinding(UIElement.IsEnabledProperty, CreateConnectionMenuBinding(nameof(NodifyGraphConnectionViewModel.CanEditRule), BindingMode.OneWay));
		blockedSites.ItemContainerStyle = CreateSiteOptionStyle(theme);
		menu.Items.Add(blockedSites);

		return menu;
	}

	private ContextMenu CreateCanvasContextMenu()
	{
		var menu = new ContextMenu();
		theme.ApplyToContextMenu(menu);
		var addLayer = new MenuItem
		{
			Header = "Add root layer...",
			IsEnabled = group.ConfigurationSource.CanEdit,
			Command = new DelegateCommand(_ => AddRootLayerFromCanvas(), _ => group.ConfigurationSource.CanEdit)
		};
		menu.Items.Add(addLayer);

		return menu;
	}

	private void AddRootLayerFromCanvas()
	{
		try
		{
			if (!group.ConfigurationSource.CanEdit)
			{
				ReportEditResult(ArchitectureConfigurationEditResult.Failure("This configuration source is not editable."));
				return;
			}

			var request = layerCreationHandler();
			if (request is null)
			{
				return;
			}

			logger?.LogInformation("Adding root layer '{LayerName}' from graph background menu.", request.Name);
			var result = ArchitectureConfigurationEditService.AddLayer(
				group.ConfigurationSource,
				string.Empty,
				request.Name,
				request.MatcherKind,
				request.MatcherAttributes);
			ReportEditResult(result);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to add root layer from graph background menu.");
			ReportEditResult(ArchitectureConfigurationEditResult.Failure("Adding the layer failed. See the graph editor log for details."));
		}
	}

	private static void ConnectionContextMenuOpened(object sender, RoutedEventArgs e)
	{
		if (sender is not ContextMenu menu)
		{
			return;
		}

		menu.DataContext = (menu.PlacementTarget as FrameworkElement)?.DataContext;
	}

	private ContextMenu CreateNodeContextMenu()
	{
		var menu = new ContextMenu();
		theme.ApplyToContextMenu(menu);
		menu.Opened += NodeContextMenuOpened;
		var addChild = new MenuItem { Header = "Add child layer..." };
		addChild.SetBinding(MenuItem.CommandProperty, CreateConnectionMenuBinding(nameof(NodifyGraphNodeViewModel.AddChildLayerCommand)));
		menu.Items.Add(addChild);
		menu.Items.Add(new Separator());
		var remove = new MenuItem { Header = "Remove layer" };
		remove.SetBinding(MenuItem.CommandProperty, CreateConnectionMenuBinding(nameof(NodifyGraphNodeViewModel.RemoveCommand)));
		menu.Items.Add(remove);

		return menu;
	}

	private static void NodeContextMenuOpened(object sender, RoutedEventArgs e)
	{
		if (sender is not ContextMenu menu)
		{
			return;
		}

		menu.DataContext = (menu.PlacementTarget as FrameworkElement)?.DataContext;
	}

	private static Style CreateSiteOptionStyle(ArchitectureGraphCanvasTheme theme)
	{
		var style = theme.CreateMenuItemStyle();
		style.Setters.Add(new Setter(HeaderedItemsControl.HeaderProperty, new Binding(nameof(NodifySiteFilterOptionViewModel.Site))));
		style.Setters.Add(new Setter(MenuItem.IsCheckableProperty, true));
		style.Setters.Add(new Setter(MenuItem.IsCheckedProperty, new Binding(nameof(NodifySiteFilterOptionViewModel.IsChecked))));
		style.Setters.Add(new Setter(MenuItem.CommandProperty, new Binding(nameof(NodifySiteFilterOptionViewModel.Command))));

		return style;
	}

	private static Binding CreateConnectionMenuBinding(string path, BindingMode mode = BindingMode.Default)
	{
		var result = new Binding(path) { Mode = mode };

		return result;
	}

	private void CompleteConnection(object? parameter)
	{
		try
		{
			if (!TryGetConnectionEndpoints(parameter, out var from, out var to))
			{
				logger?.LogWarning("Could not resolve connection endpoints from Nodify parameter of type {ParameterType}.", parameter?.GetType().FullName ?? "<null>");
				ReportEditResult(ArchitectureConfigurationEditResult.Failure("Drag from an out connector to an in connector to add an AllowedDependency."));
				return;
			}

			if (string.Equals(from, to, StringComparison.Ordinal))
			{
				logger?.LogInformation("Rejected self-connection for layer '{LayerPath}'.", from);
				ReportEditResult(ArchitectureConfigurationEditResult.Failure("A layer cannot be connected to itself from the graph editor."));
				return;
			}

			logger?.LogInformation("Adding AllowedDependency from '{From}' to '{To}' from graph gesture.", from, to);
			var result = ArchitectureConfigurationEditService.AddAllowedDependency(group.ConfigurationSource, from, to);
			ReportEditResult(result);
		}
		catch (Exception exception)
		{
			logger?.LogError(exception, "Failed to complete graph connection gesture.");
			ReportEditResult(ArchitectureConfigurationEditResult.Failure("Adding the dependency failed. See the graph editor log for details."));
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

	private static bool TryGetConnectionEndpoints(object? parameter, out string from, out string to)
	{
		if (!TryGetTupleItems(parameter, out var first, out var second))
		{
			from = string.Empty;
			to = string.Empty;
			return false;
		}

		if (first is NodifyGraphConnectorViewModel firstConnector && second is NodifyGraphConnectorViewModel secondConnector)
		{
			return TryGetConnectionEndpoints(firstConnector, secondConnector, out from, out to);
		}

		from = string.Empty;
		to = string.Empty;
		return false;
	}

	private static bool TryGetTupleItems(object? parameter, out object? first, out object? second)
	{
		if (parameter is Tuple<object, object> tuple)
		{
			first = tuple.Item1;
			second = tuple.Item2;
			return true;
		}

		var type = parameter?.GetType();
		var firstProperty = type?.GetProperty("Item1");
		var secondProperty = type?.GetProperty("Item2");
		if (parameter is not null && firstProperty is not null && secondProperty is not null)
		{
			first = firstProperty.GetValue(parameter);
			second = secondProperty.GetValue(parameter);
			return true;
		}

		first = null;
		second = null;
		return false;
	}

	private static bool TryGetConnectionEndpoints(NodifyGraphConnectorViewModel first, NodifyGraphConnectorViewModel second, out string from, out string to)
	{
		if (first.IsOutput && second.IsInput)
		{
			from = first.LayerPath;
			to = second.LayerPath;
			return true;
		}

		if (first.IsInput && second.IsOutput)
		{
			from = second.LayerPath;
			to = first.LayerPath;
			return true;
		}

		from = string.Empty;
		to = string.Empty;
		return false;
	}
}
