using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddRootConfigurationEditor(StackPanel panel, ArchitectureConfigurationSource source)
	{
		panel.Children.Add(CreateSectionTitle("Configuration"));
		AddReadOnlyRow(panel, "Source", source.CanEdit ? source.Path : "Not editable");
		panel.Children.Add(CreateHintTextBlock("Edit the selected XML or inline settings from here. Changes are saved immediately to the configuration source.", new Thickness(0, 6, 0, 0)));
		var details = ArchitectureConfigurationEditService.GetRootDetails(source);
		if (!details.Succeeded)
		{
			panel.Children.Add(new TextBlock { Text = details.Message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), Foreground = Brushes.IndianRed });
			return;
		}

		var description = CreateDescriptionBox(details.Description, source.CanEdit);
		panel.Children.Add(CreateSectionTitle("Description"));
		panel.Children.Add(description);
		var requireRecognized = new TextBox { Text = details.RequireRecognizedDependencies ?? string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(CreateSectionTitle("requireRecognizedDependencies"));
		panel.Children.Add(requireRecognized);
		var enforceAcyclic = new CheckBox { Content = "enforceAcyclic", IsChecked = details.EnforceAcyclic, Margin = new Thickness(0, 8, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(enforceAcyclic);
		var enableReport = new CheckBox { Content = "enableReport", IsChecked = details.EnableReport, Margin = new Thickness(0, 8, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(enableReport);
		var reportPath = new TextBox { Text = details.ReportPath ?? string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(CreateSectionTitle("reportPath"));
		panel.Children.Add(reportPath);
		var enableDocumentation = new CheckBox { Content = "enableDocumentation", IsChecked = details.EnableDocumentation, Margin = new Thickness(0, 8, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(enableDocumentation);
		var documentationPath = new TextBox { Text = details.DocumentationPath ?? string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(CreateSectionTitle("documentationPath"));
		panel.Children.Add(documentationPath);
		ArchitectureConfigurationEditResult SaveRootSettings()
		{
			return ArchitectureConfigurationEditService.SetRootSettings(
			source,
			description.Text,
			requireRecognized.Text,
			enforceAcyclic.IsChecked == true,
			enableReport.IsChecked == true,
			reportPath.Text,
			enableDocumentation.IsChecked == true,
			documentationPath.Text);
		}

		AutoSaveOnLostFocus(description, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(requireRecognized, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enforceAcyclic, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enableReport, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(reportPath, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enableDocumentation, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(documentationPath, SaveRootSettings, source.CanEdit);
		AddLayerCreationEditor(panel, source, string.Empty, "Root layers");
		AddIncludeEditors(panel, source, details.Includes);
		AddGlobalConfigurationElementEditors(panel, "Global allowed type policy", details.AllowedPolicies, source, "Allowed");
		AddGlobalConfigurationElementEditors(panel, "Global forbidden type policy", details.ForbiddenPolicies, source, "Forbidden");
	}
}
