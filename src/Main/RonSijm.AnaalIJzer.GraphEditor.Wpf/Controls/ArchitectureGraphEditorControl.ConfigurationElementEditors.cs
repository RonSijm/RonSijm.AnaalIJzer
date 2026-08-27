using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddConfigurationElementEditors(
		StackPanel panel,
		string title,
		ImmutableArray<ArchitectureConfigurationElementDetails> elements,
		ArchitectureLayerEditHandle layerHandle,
		string containerKind,
		ImmutableArray<string> elementKinds)
	{
		panel.Children.Add(CreateSectionTitle(title));
		if (elements.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var element in elements)
		{
			panel.Children.Add(CreateConfigurationElementEditor(element));
		}

		var addPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
		addPanel.Children.Add(new TextBlock { Text = "Add", FontWeight = FontWeights.SemiBold });
		var kind = new ComboBox { Margin = new Thickness(0, 2, 0, 0), IsEnabled = layerHandle.CanEdit };
		foreach (var elementKind in elementKinds)
		{
			kind.Items.Add(elementKind);
		}

		kind.SelectedIndex = 0;
		addPanel.Children.Add(kind);
		var attributeEditor = CreateMatcherAttributeEditor(kind, layerHandle.CanEdit);
		addPanel.Children.Add(attributeEditor.Panel);
		var add = new Button { Content = "Add " + title.ToLowerInvariant(), Margin = new Thickness(0, 4, 0, 0), IsEnabled = layerHandle.CanEdit };
		add.Click += (_, _) =>
		{
			if (!attributeEditor.TryGetAttributes(out var parsedAttributes, out var message))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure(message));
				return;
			}

			var elementKind = kind.SelectedItem as string ?? "Class";
			var result = containerKind == "LayerMatcher"
				? _editService.AddLayerMatcher(layerHandle, elementKind, parsedAttributes)
				: _editService.AddTypePolicyMatcher(layerHandle, containerKind, elementKind, parsedAttributes);
			HandleEditResult(result);
		};
		addPanel.Children.Add(add);
		panel.Children.Add(addPanel);
	}

	private void AddReadOnlyConfigurationElementEditors(
		StackPanel panel,
		string title,
		ImmutableArray<ArchitectureConfigurationElementDetails> elements)
	{
		panel.Children.Add(CreateSectionTitle(title));
		if (elements.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
			return;
		}

		panel.Children.Add(CreateHintTextBlock("Edit exception metadata here. Add new exception branches from the owning matcher or policy entry.", new Thickness(0, 0, 0, 4)));
		foreach (var element in elements)
		{
			panel.Children.Add(CreateConfigurationElementEditor(element));
		}
	}
}
