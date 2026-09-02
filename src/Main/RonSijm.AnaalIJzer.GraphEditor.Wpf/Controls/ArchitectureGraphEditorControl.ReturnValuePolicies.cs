using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddReturnValuePolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("Return-value policies"));
		panel.Children.Add(CreateHintTextBlock("Forbid direct returned expressions. Use Literal value=\"null\", Literal value=\"\", Literal value=\"42\", or an Invocation matcher such as withAttribute=\"JetBrains.Annotations.CanBeNullAttribute\". Attributes on one matcher are combined.", new Thickness(0, 0, 0, 4)));
		if (policies.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var policy in policies)
		{
			panel.Children.Add(CreateReturnValuePolicyEditor(policy));
		}

		panel.Children.Add(CreateNewReturnValuePolicyEditor(handle));
	}

	private UIElement CreateReturnValuePolicyEditor(ArchitectureConfigurationElementDetails policy)
	{
		var expander = new Expander
		{
			Header = policy.Summary,
			IsExpanded = false,
			Margin = new Thickness(0, 4, 0, 0)
		};
		var panel = new StackPanel();
		var canEdit = policy.Handle.CanEdit;
		var description = CreateDescriptionBox(policy.Attributes.TryGetValue("description", out var configuredDescription) ? configuredDescription : null, canEdit);
		var matcherXml = CreateReturnValueMatcherXmlBox(policy.ChildXml, canEdit);

		ArchitectureConfigurationEditResult Save()
		{
			if (string.IsNullOrWhiteSpace(matcherXml.Text))
			{
				return ArchitectureConfigurationEditResult.Failure("ReturnValuePolicy requires at least one forbidden return matcher.");
			}

			var attributesResult = _editService.SetConfigurationElementAttributes(policy.Handle, CreateReturnValuePolicyAttributes(description.Text));
			if (!attributesResult.Succeeded)
			{
				return attributesResult;
			}

			var result = _editService.SetConfigurationElementChildren(policy.Handle, matcherXml.Text);

			return result;
		}

		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);
		panel.Children.Add(new TextBlock { Text = "Forbidden returned expressions", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(CreateHintTextBlock("One XML matcher element per forbidden direct return. Supported elements: Literal, Invocation, New, Identifier, and MemberAccess.", new Thickness(0, 0, 0, 2)));
		panel.Children.Add(matcherXml);

		AutoSaveOnLostFocus(description, Save, canEdit);
		AutoSaveOnLostFocus(matcherXml, Save, canEdit);

		var remove = CreateDangerButton("Remove return-value policy", canEdit);
		remove.Margin = new Thickness(0, 8, 0, 0);
		remove.Click += (_, _) =>
		{
			if (_confirmationHandler("Remove '" + policy.Summary + "'?"))
			{
				HandleEditResult(_editService.RemoveConfigurationElement(policy.Handle), true);
			}
		};
		panel.Children.Add(remove);
		expander.Content = panel;

		return expander;
	}

	private UIElement CreateNewReturnValuePolicyEditor(ArchitectureLayerEditHandle handle)
	{
		var expander = new Expander
		{
			Header = "Add return-value policy",
			IsExpanded = false,
			Margin = new Thickness(0, 8, 0, 0)
		};
		var panel = new StackPanel();
		var matcherXml = CreateReturnValueMatcherXmlBox("<Literal value=\"null\" />", handle.CanEdit);
		var description = CreateDescriptionBox(null, handle.CanEdit);

		panel.Children.Add(new TextBlock { Text = "Forbidden returned expressions", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(matcherXml);
		panel.Children.Add(CreateHintTextBlock("Start with the null literal, or replace it with one or more matchers such as <Literal value=\"\" /> or <Invocation withAttribute=\"JetBrains.Annotations.CanBeNullAttribute\" />.", new Thickness(0, 2, 0, 0)));
		panel.Children.Add(new TextBlock { Text = "Policy description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);

		var add = new Button { Content = "Add return-value policy", IsEnabled = handle.CanEdit, Margin = new Thickness(0, 8, 0, 0) };
		add.Click += (_, _) =>
		{
			var result = _editService.AddReturnValuePolicy(handle, CreateReturnValuePolicyAttributes(description.Text), matcherXml.Text);
			HandleEditResult(result, true);
		};
		panel.Children.Add(add);
		expander.Content = panel;

		return expander;
	}

	private static TextBox CreateReturnValueMatcherXmlBox(string text, bool isEnabled)
	{
		var result = new TextBox
		{
			Text = text,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			MinHeight = 88,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			IsEnabled = isEnabled
		};

		return result;
	}

	private static ImmutableDictionary<string, string> CreateReturnValuePolicyAttributes(string? description)
	{
		var attributes = ImmutableDictionary<string, string>.Empty;
		if (description is { Length: > 0 } && !string.IsNullOrWhiteSpace(description))
		{
			attributes = attributes.Add("description", description);
		}

		return attributes;
	}
}
