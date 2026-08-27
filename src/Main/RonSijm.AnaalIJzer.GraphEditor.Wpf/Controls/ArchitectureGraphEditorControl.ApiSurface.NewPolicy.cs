using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private UIElement CreateNewApiSurfacePolicyEditor(ArchitectureLayerEditHandle handle)
	{
		var expander = new Expander { Header = "Add API surface policy", IsExpanded = false, Margin = new Thickness(0, 8, 0, 0) };
		var panel = new StackPanel();
		var requireRecognized = new CheckBox { Content = "Require recognized exposed types", IsEnabled = handle.CanEdit };
		var enableTransitive = new CheckBox { Content = "Inspect public object graph", IsEnabled = handle.CanEdit };
		var transitiveDepth = new TextBox { Text = "3", IsEnabled = handle.CanEdit };
		var description = CreateDescriptionBox(null, handle.CanEdit);
		var mode = new ComboBox { IsEnabled = handle.CanEdit };
		mode.Items.Add("Allow");
		mode.Items.Add("Block");
		mode.SelectedIndex = 1;
		var path = new TextBox { Text = "/RepositoryQuerySurface", IsEnabled = handle.CanEdit };

		panel.Children.Add(requireRecognized);
		panel.Children.Add(enableTransitive);
		panel.Children.Add(new TextBlock { Text = "Maximum transitive depth (1-10)", Margin = new Thickness(0, 4, 0, 2) });
		panel.Children.Add(transitiveDepth);
		panel.Children.Add(new TextBlock { Text = "First rule", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(mode);
		panel.Children.Add(path);
		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);

		var add = new Button { Content = "Add API surface policy", IsEnabled = handle.CanEdit, Margin = new Thickness(0, 8, 0, 0) };
		add.Click += (_, _) =>
		{
			var result = CreateNewApiSurfacePolicy(handle, requireRecognized.IsChecked == true, enableTransitive.IsChecked == true, transitiveDepth.Text, description.Text, mode.SelectedIndex, path.Text);
			HandleEditResult(result, true);
		};
		panel.Children.Add(add);
		expander.Content = panel;

		return expander;
	}

	private ArchitectureConfigurationEditResult CreateNewApiSurfacePolicy(ArchitectureLayerEditHandle handle, bool requireRecognized, bool enableTransitive, string transitiveDepth, string description, int modeIndex, string path)
	{
		var elementName = modeIndex == 0 ? "AllowedLayer" : "BlockedLayer";
		var child = new XElement(elementName, new XAttribute("path", path.Trim()));
		var transitiveElement = CreateTransitiveExposureElement(enableTransitive, transitiveDepth, null);
		var childXml = transitiveElement is null
			? child.ToString(SaveOptions.DisableFormatting)
			: transitiveElement.ToString(SaveOptions.DisableFormatting) + Environment.NewLine + child.ToString(SaveOptions.DisableFormatting);
		var result = string.IsNullOrWhiteSpace(path)
			? ArchitectureConfigurationEditResult.Failure("Enter a canonical layer path.")
			: enableTransitive && transitiveElement is null
				? ArchitectureConfigurationEditResult.Failure("Transitive exposure depth must be a whole number from 1 through 10.")
				: _editService.AddApiSurfacePolicy(
					handle,
					CreateApiSurfaceAttributes(requireRecognized, description),
					childXml);

		return result;
	}
}
