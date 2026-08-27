using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddRootConfigurationEditor(StackPanel panel, ArchitectureConfigurationSource source)
	{
		panel.Children.Add(CreateSectionTitle("Configuration"));
		AddReadOnlyRow(panel, "Source", source.CanEdit ? source.Path : "Not editable");
		panel.Children.Add(CreateHintTextBlock("Edit the selected XML or inline settings from here. Changes are saved immediately to the configuration source.", new Thickness(0, 6, 0, 0)));
		var details = _editService.GetRootDetails(source);
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
		var enableExceptionPolicy = new CheckBox { Content = "Enable exception policy", IsChecked = details.EnableExceptionPolicy, Margin = new Thickness(0, 8, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(enableExceptionPolicy);
		var requireExceptionReason = new CheckBox { Content = "requireReason", IsChecked = details.RequireExceptionReason, Margin = new Thickness(16, 4, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(requireExceptionReason);
		var requireExceptionOwner = new CheckBox { Content = "requireOwner", IsChecked = details.RequireExceptionOwner, Margin = new Thickness(16, 4, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(requireExceptionOwner);
		var requireExceptionExpiresOn = new CheckBox { Content = "requireExpiresOn", IsChecked = details.RequireExceptionExpiresOn, Margin = new Thickness(16, 4, 0, 0), IsEnabled = source.CanEdit };
		panel.Children.Add(requireExceptionExpiresOn);
		var exceptionWarnBeforeDays = new TextBox { Text = details.ExceptionWarnBeforeDays.ToString(), TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(CreateSectionTitle("warnBeforeDays"));
		panel.Children.Add(exceptionWarnBeforeDays);
		var exceptionPolicyDescription = CreateDescriptionBox(details.ExceptionPolicyDescription, source.CanEdit);
		panel.Children.Add(CreateSectionTitle("Exception policy description"));
		panel.Children.Add(exceptionPolicyDescription);
		ArchitectureConfigurationEditResult SaveRootSettings()
		{
			var parsedWarnBeforeDays = int.TryParse(exceptionWarnBeforeDays.Text, out var warnBeforeDays) ? warnBeforeDays : 14;
			return _editService.SetRootSettings(
			source,
			description.Text,
			requireRecognized.Text,
			enforceAcyclic.IsChecked == true,
			enableReport.IsChecked == true,
			reportPath.Text,
			enableDocumentation.IsChecked == true,
			documentationPath.Text,
			enableExceptionPolicy.IsChecked == true,
			requireExceptionReason.IsChecked == true,
			requireExceptionOwner.IsChecked == true,
			requireExceptionExpiresOn.IsChecked == true,
			parsedWarnBeforeDays,
			exceptionPolicyDescription.Text);
		}

		AutoSaveOnLostFocus(description, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(requireRecognized, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enforceAcyclic, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enableReport, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(reportPath, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enableDocumentation, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(documentationPath, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(enableExceptionPolicy, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(requireExceptionReason, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(requireExceptionOwner, SaveRootSettings, source.CanEdit);
		AutoSaveOnCheckChanged(requireExceptionExpiresOn, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(exceptionWarnBeforeDays, SaveRootSettings, source.CanEdit);
		AutoSaveOnLostFocus(exceptionPolicyDescription, SaveRootSettings, source.CanEdit);
		AddReadOnlyConfigurationElementEditors(panel, "Exception matchers", details.ExceptionMatchers);
		AddExceptionReviewSection(panel, null);
		AddLayerCreationEditor(panel, source, string.Empty, "Root layers");
		AddIncludeEditors(panel, source, details.Includes);
		AddGlobalConfigurationElementEditors(panel, "Global allowed type policy", details.AllowedPolicies, source, "Allowed");
		AddGlobalConfigurationElementEditors(panel, "Global forbidden type policy", details.ForbiddenPolicies, source, "Forbidden");
	}
}
