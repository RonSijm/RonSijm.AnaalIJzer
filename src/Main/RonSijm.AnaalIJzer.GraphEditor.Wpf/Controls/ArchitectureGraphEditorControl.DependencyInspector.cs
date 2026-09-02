using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private UIElement CreateDependencyRuleInspector(ArchitectureGraphSelection selection)
	{
		var handle = selection.DependencyHandle;
		var panel = CreateInspectorShell(selection);
		AddReadOnlyRow(panel, "From", handle.ConfiguredFrom);
		AddReadOnlyRow(panel, "To", handle.ConfiguredTo);
		AddReadOnlyRow(panel, "Scope", string.IsNullOrWhiteSpace(handle.ScopePath) ? "root" : handle.ScopePath);
		AddReadOnlyRow(panel, "Source", handle.CanEdit ? handle.SourcePath : "Not editable");
		AddConfigurationFixesEditor(panel, selection);
		panel.Children.Add(CreateSectionTitle("Rule kind"));
		var kindBox = new ComboBox { Margin = new Thickness(0, 2, 0, 0), IsEnabled = handle.CanEdit };
		kindBox.Items.Add("AllowedDependency");
		kindBox.Items.Add("BlockedDependency");
		kindBox.SelectedItem = handle.ElementKind;
		panel.Children.Add(kindBox);
		AutoSaveOnSelectionChanged(kindBox, () => _editService.SetDependencyKind(handle, kindBox.SelectedItem as string ?? handle.ElementKind), handle.CanEdit, refresh: false);
		var cascade = new CheckBox { Content = "appliesToDescendants", IsChecked = handle.AppliesToDescendants, Margin = new Thickness(0, 10, 0, 0), IsEnabled = handle.CanEdit };
		panel.Children.Add(cascade);
		AutoSaveOnCheckChanged(cascade, () => _editService.SetDependencyAppliesToDescendants(handle, cascade.IsChecked == true), handle.CanEdit, refresh: false);
		var description = CreateDescriptionBox(handle.Description, handle.CanEdit);
		panel.Children.Add(CreateSectionTitle("Description"));
		panel.Children.Add(description);
		AutoSaveOnLostFocus(description, () => _editService.SetDependencyDescription(handle, description.Text), handle.CanEdit, refreshOnLostFocus: false);
		AddSiteEditor(panel, handle, selection);

		return panel;
	}

	private void AddSiteEditor(StackPanel panel, ArchitectureDependencyRuleEditHandle handle, ArchitectureGraphSelection selection)
	{
		panel.Children.Add(CreateSectionTitle("Sites"));
		var allowedChecks = CreateSiteChecks(selection.AllowedSites, handle.CanEdit);
		var blockedChecks = CreateSiteChecks(selection.BlockedSites, handle.CanEdit);
		var allSites = new Button { Content = "Allow all sites", Margin = new Thickness(0, 4, 0, 0), IsEnabled = handle.CanEdit };
		allSites.Click += (_, _) => HandleEditResult(_editService.SetDependencySites(handle, ArchitectureSiteFilterEditMode.All, ImmutableArray<string>.Empty));
		panel.Children.Add(allSites);
		panel.Children.Add(new TextBlock { Text = "allowedSites", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(allowedChecks.Panel);
		AutoSaveOnSiteChecks(allowedChecks.Checks, () => _editService.SetDependencySites(handle, ArchitectureSiteFilterEditMode.AllowedSites, GetCheckedSites(allowedChecks.Checks)), handle.CanEdit, refresh: false);
		panel.Children.Add(new TextBlock { Text = "blockedSites", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(blockedChecks.Panel);
		AutoSaveOnSiteChecks(blockedChecks.Checks, () => _editService.SetDependencySites(handle, ArchitectureSiteFilterEditMode.BlockedSites, GetCheckedSites(blockedChecks.Checks)), handle.CanEdit, refresh: false);
	}
}
