using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private UIElement CreateApiSurfacePolicyEditor(ArchitectureConfigurationElementDetails policy)
	{
		var expander = new Expander { Header = policy.Summary, IsExpanded = false, Margin = new Thickness(0, 4, 0, 0) };
		var panel = new StackPanel();
		var canEdit = policy.Handle.CanEdit;
		var requireRecognized = new CheckBox
		{
			Content = "Require exposed types to belong to a configured layer",
			IsChecked = ParseBoolean(policy.Attributes, "requireRecognizedTypes"),
			IsEnabled = canEdit
		};
		var description = CreateDescriptionBox(policy.Attributes.TryGetValue("description", out var configuredDescription) ? configuredDescription : null, canEdit);
		var transitive = ParseTransitiveExposure(policy.ChildXml);
		var enableTransitive = new CheckBox
		{
			Content = "Inspect the public object graph of exposed types",
			IsChecked = transitive is not null,
			IsEnabled = canEdit
		};
		var transitiveDepth = new TextBox
		{
			Text = transitive?.Attribute("maxDepth")?.Value ?? "3",
			IsEnabled = canEdit,
			MinWidth = 48
		};
		var transitiveDescription = CreateDescriptionBox(transitive?.Attribute("description")?.Value, canEdit);
		var rulesPanel = new StackPanel();
		var ruleEditors = ParseApiSurfaceRules(policy.ChildXml).Select(rule => CreateApiSurfaceRuleEditor(rule, canEdit, rulesPanel)).ToList();
		foreach (var ruleEditor in ruleEditors)
		{
			rulesPanel.Children.Add(ruleEditor.Root);
		}

		ArchitectureConfigurationEditResult Save()
		{
			var transitiveElement = CreateTransitiveExposureElement(enableTransitive.IsChecked == true, transitiveDepth.Text, transitiveDescription.Text);
			if (enableTransitive.IsChecked == true && transitiveElement is null)
			{
				return ArchitectureConfigurationEditResult.Failure("Transitive exposure depth must be a whole number from 1 through 10.");
			}

			var attributes = CreateApiSurfaceAttributes(requireRecognized.IsChecked == true, description.Text);
			var attributeResult = editService.SetConfigurationElementAttributes(policy.Handle, attributes);
			if (!attributeResult.Succeeded)
			{
				return attributeResult;
			}

			var children = ruleEditors.Select(editor => editor.CreateElement()).ToList();
			if (transitiveElement is not null)
			{
				children.Insert(0, transitiveElement);
			}

			var childXml = string.Join(Environment.NewLine, children.Select(element => element.ToString(SaveOptions.DisableFormatting)));
			var result = editService.SetConfigurationElementChildren(policy.Handle, childXml);

			return result;
		}

		foreach (var ruleEditor in ruleEditors)
		{
			AttachApiSurfaceRuleAutoSave(ruleEditor, ruleEditors, rulesPanel, Save);
		}

		panel.Children.Add(requireRecognized);
		panel.Children.Add(new TextBlock { Text = "Transitive exposure", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(enableTransitive);
		panel.Children.Add(new TextBlock { Text = "Maximum public object-graph depth (1-10)", Margin = new Thickness(0, 4, 0, 2) });
		panel.Children.Add(transitiveDepth);
		panel.Children.Add(new TextBlock { Text = "Transitive rule description", Margin = new Thickness(0, 4, 0, 2) });
		panel.Children.Add(transitiveDescription);
		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);
		panel.Children.Add(new TextBlock { Text = "Layer rules", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(rulesPanel);
		panel.Children.Add(CreateAddApiSurfaceRuleButton(canEdit, rulesPanel, ruleEditors, Save));

		AutoSaveOnCheckChanged(requireRecognized, Save, canEdit);
		AutoSaveOnCheckChanged(enableTransitive, Save, canEdit);
		AutoSaveOnLostFocus(transitiveDepth, Save, canEdit);
		AutoSaveOnLostFocus(transitiveDescription, Save, canEdit);
		AutoSaveOnLostFocus(description, Save, canEdit);

		var remove = CreateDangerButton("Remove API surface policy", canEdit);
		remove.Margin = new Thickness(0, 8, 0, 0);
		remove.Click += (_, _) =>
		{
			if (confirmationHandler("Remove '" + policy.Summary + "'?"))
			{
				HandleEditResult(editService.RemoveConfigurationElement(policy.Handle), true);
			}
		};
		panel.Children.Add(remove);
		expander.Content = panel;

		return expander;
	}

	private Button CreateAddApiSurfaceRuleButton(bool canEdit, StackPanel rulesPanel, List<ApiSurfaceRuleEditor> ruleEditors, Func<ArchitectureConfigurationEditResult> save)
	{
		var addRule = new Button { Content = "Add layer rule", IsEnabled = canEdit, Margin = new Thickness(0, 6, 0, 0) };
		addRule.Click += (_, _) =>
		{
			var ruleEditor = CreateApiSurfaceRuleEditor(new XElement("AllowedLayer", new XAttribute("path", "/Layer")), canEdit, rulesPanel);
			ruleEditors.Add(ruleEditor);
			rulesPanel.Children.Add(ruleEditor.Root);
			AttachApiSurfaceRuleAutoSave(ruleEditor, ruleEditors, rulesPanel, save);
		};

		return addRule;
	}

	private void AttachApiSurfaceRuleAutoSave(ApiSurfaceRuleEditor ruleEditor, List<ApiSurfaceRuleEditor> ruleEditors, StackPanel rulesPanel, Func<ArchitectureConfigurationEditResult> save)
	{
		ruleEditor.AttachAutoSave(save);
		ruleEditor.RemoveButton.Click += (_, _) =>
		{
			ruleEditors.Remove(ruleEditor);
			rulesPanel.Children.Remove(ruleEditor.Root);
			HandleAutoSaveResult(save());
		};
	}
}
