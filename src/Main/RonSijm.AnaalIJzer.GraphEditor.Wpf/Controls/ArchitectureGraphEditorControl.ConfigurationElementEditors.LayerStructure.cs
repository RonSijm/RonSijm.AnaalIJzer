using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddLayerStructureEditor(StackPanel panel, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("Structure"));
		var source = new ArchitectureConfigurationSource(handle.SourceKind, handle.SourcePath);
		AddLayerCreationEditor(panel, source, handle.LayerPath, "Child layers");
		panel.Children.Add(new TextBlock { Text = "Move to parent path", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		var moveTarget = new TextBox
		{
			Text = handle.ParentPath,
			TextWrapping = TextWrapping.Wrap,
			IsEnabled = handle.CanEdit
		};
		panel.Children.Add(moveTarget);
		var move = new Button { Content = "Move layer", Margin = new Thickness(0, 4, 0, 0), IsEnabled = handle.CanEdit };
		move.Click += (_, _) =>
		{
			if (confirmationHandler("Move layer '" + handle.LayerPath + "' to parent path '" + (string.IsNullOrWhiteSpace(moveTarget.Text) ? "root" : moveTarget.Text.Trim()) + "'?"))
			{
				HandleEditResult(editService.MoveLayer(handle, moveTarget.Text), true);
			}
		};
		panel.Children.Add(move);
		var remove = CreateDangerButton("Remove layer", handle.CanEdit);
		remove.Margin = new Thickness(0, 8, 0, 0);
		remove.Click += (_, _) =>
		{
			if (confirmationHandler("Remove layer '" + handle.LayerPath + "' and its nested settings?"))
			{
				HandleEditResult(editService.RemoveLayer(handle), true);
			}
		};
		panel.Children.Add(remove);
	}

	private void AddNameRuleEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> elements, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("Name rules"));
		panel.Children.Add(CreateHintTextBlock("Movement rules compare source and target values. Declaration rules compare a semantic type with its declaration name.", new Thickness(0, 0, 0, 4)));
		if (elements.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var element in elements)
		{
			panel.Children.Add(CreateConfigurationElementEditor(element));
		}

		var kind = new ComboBox { Margin = new Thickness(0, 4, 0, 0), IsEnabled = handle.CanEdit };
		kind.Items.Add("RequireMatchingNames");
		kind.Items.Add("RequireDeclarationNameMatchesType");
		kind.SelectedIndex = 0;
		panel.Children.Add(kind);
		var allowedSites = new TextBox { Text = string.Empty, Margin = new Thickness(0, 4, 0, 0), IsEnabled = handle.CanEdit };
		panel.Children.Add(CreateHintTextBlock("Optional allowedSites, for example Method, Property.", new Thickness(0, 4, 0, 2)));
		panel.Children.Add(allowedSites);
		var add = new Button { Content = "Add name rule", Margin = new Thickness(0, 4, 0, 0), IsEnabled = handle.CanEdit };
		add.Click += (_, _) =>
		{
			var attributes = string.IsNullOrWhiteSpace(allowedSites.Text)
				? ImmutableDictionary<string, string>.Empty
				: ImmutableDictionary<string, string>.Empty.Add("allowedSites", allowedSites.Text.Trim());
			HandleEditResult(editService.AddNameRule(handle, kind.SelectedItem?.ToString() ?? "RequireMatchingNames", attributes), true);
		};
		panel.Children.Add(add);
	}

	private void AddLayerCreationEditor(StackPanel panel, ArchitectureConfigurationSource source, string parentPath, string title)
	{
		panel.Children.Add(CreateSectionTitle(title));
		panel.Children.Add(new TextBlock { Text = "New layer name", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2) });
		var name = new TextBox { Text = string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(name);
		var matcherKind = new ComboBox { Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		matcherKind.Items.Add("Class");
		matcherKind.Items.Add("Namespace");
		matcherKind.Items.Add("Assembly");
		matcherKind.SelectedIndex = 0;
		panel.Children.Add(matcherKind);
		var matcherAttributes = CreateMatcherAttributeEditor(matcherKind, source.CanEdit);
		panel.Children.Add(matcherAttributes.Panel);
		var add = new Button { Content = "Add layer", Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) =>
		{
			if (!matcherAttributes.TryGetAttributes(out var parsedAttributes, out var message))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure(message));
				return;
			}

			HandleEditResult(editService.AddLayer(source, parentPath, name.Text, matcherKind.SelectedItem?.ToString() ?? "Class", parsedAttributes));
		};
		panel.Children.Add(add);
	}
}
