using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddGlobalConfigurationElementEditors(
		StackPanel panel,
		string title,
		ImmutableArray<ArchitectureConfigurationElementDetails> elements,
		ArchitectureConfigurationSource source,
		string policyKind)
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
		var kind = new ComboBox { Margin = new Thickness(0, 2, 0, 0), IsEnabled = source.CanEdit };
		kind.Items.Add("Class");
		kind.Items.Add("Namespace");
		kind.SelectedIndex = 0;
		addPanel.Children.Add(kind);
		var attributeEditor = CreateMatcherAttributeEditor(kind, source.CanEdit);
		addPanel.Children.Add(attributeEditor.Panel);
		var add = new Button { Content = "Add " + title.ToLowerInvariant(), Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) =>
		{
			if (!attributeEditor.TryGetAttributes(out var parsedAttributes, out var message))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure(message));
				return;
			}

			HandleEditResult(_editService.AddGlobalTypePolicyMatcher(source, policyKind, kind.SelectedItem?.ToString() ?? "Class", parsedAttributes));
		};
		addPanel.Children.Add(add);
		panel.Children.Add(addPanel);
	}

	private void AddIncludeEditors(StackPanel panel, ArchitectureConfigurationSource source, ImmutableArray<ArchitectureConfigurationElementDetails> includes)
	{
		panel.Children.Add(CreateSectionTitle("Includes"));
		if (includes.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var include in includes)
		{
			panel.Children.Add(CreateConfigurationElementEditor(include));
		}

		var path = new TextBox { Text = string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(path);
		var add = new Button { Content = "Add Include", Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) => HandleEditResult(_editService.AddInclude(source, path.Text));
		panel.Children.Add(add);
	}
}
