using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
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
			var result = editService.AddLayer(
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
}
