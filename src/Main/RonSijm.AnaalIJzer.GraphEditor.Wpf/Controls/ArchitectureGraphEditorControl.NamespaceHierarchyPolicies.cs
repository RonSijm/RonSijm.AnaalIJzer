using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddNamespaceHierarchyPolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureConfigurationSource source)
	{
		panel.Children.Add(CreateSectionTitle("Namespace hierarchy policies"));
		panel.Children.Add(CreateHintTextBlock("These global rules protect namespace ownership without requiring layer membership.", new Thickness(0, 0, 0, 4)));
		if (policies.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var policy in policies)
		{
			panel.Children.Add(CreateConfigurationElementEditor(policy));
		}

		var expander = new Expander
		{
			Header = "Add namespace hierarchy policy",
			Margin = new Thickness(0, 8, 0, 0),
			IsEnabled = source.CanEdit
		};
		var content = new StackPanel();
		var rootNamespace = new TextBox { Text = string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		content.Children.Add(CreateSectionTitle("rootNamespace"));
		content.Children.Add(rootNamespace);
		var description = CreateDescriptionBox(null, source.CanEdit);
		content.Children.Add(CreateSectionTitle("Description"));
		content.Children.Add(description);
		var rulesXml = new TextBox
		{
			Text = "<BlockedRelation relation=\"DescendantToAncestor\" />",
			AcceptsReturn = true,
			MinHeight = 52,
			TextWrapping = TextWrapping.Wrap,
			IsEnabled = source.CanEdit
		};
		content.Children.Add(CreateSectionTitle("BlockedRelation XML"));
		content.Children.Add(rulesXml);
		var add = new Button { Content = "Add namespace hierarchy policy", Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) =>
		{
			var attributes = ImmutableDictionary<string, string>.Empty.Add("rootNamespace", rootNamespace.Text.Trim());
			if (!string.IsNullOrWhiteSpace(description.Text))
			{
				attributes = attributes.Add("description", description.Text.Trim());
			}

			HandleEditResult(_editService.AddNamespaceHierarchyPolicy(source, attributes, rulesXml.Text), true);
		};
		content.Children.Add(add);
		expander.Content = content;
		panel.Children.Add(expander);
	}
}
