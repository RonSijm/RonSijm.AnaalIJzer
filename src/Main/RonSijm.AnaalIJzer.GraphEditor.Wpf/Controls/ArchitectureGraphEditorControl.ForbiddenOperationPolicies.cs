using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
    private void AddForbiddenOperationPolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureLayerEditHandle handle)
    {
        panel.Children.Add(CreateSectionTitle("Forbidden operation policies"));
        panel.Children.Add(CreateHintTextBlock("Block selected resolved API members without blocking their whole containing type. Each ForbiddenOperation may contain one or more OperationMatcher alternatives.", new Thickness(0, 0, 0, 4)));
        if (policies.Length == 0)
        {
            panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
        }

        foreach (var policy in policies)
        {
            panel.Children.Add(CreateForbiddenOperationPolicyEditor(policy));
        }

        panel.Children.Add(CreateNewForbiddenOperationPolicyEditor(handle));
    }

    private UIElement CreateForbiddenOperationPolicyEditor(ArchitectureConfigurationElementDetails policy)
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
        var ruleXml = CreateForbiddenOperationRuleXmlBox(policy.ChildXml, canEdit);

        ArchitectureConfigurationEditResult Save()
        {
            if (string.IsNullOrWhiteSpace(ruleXml.Text))
            {
                return ArchitectureConfigurationEditResult.Failure("ForbiddenOperations requires at least one ForbiddenOperation rule.");
            }

            var attributesResult = _editService.SetConfigurationElementAttributes(policy.Handle, CreateForbiddenOperationPolicyAttributes(description.Text));
            if (!attributesResult.Succeeded)
            {
                return attributesResult;
            }

            var result = _editService.SetConfigurationElementChildren(policy.Handle, ruleXml.Text);

            return result;
        }

        panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);
        panel.Children.Add(new TextBlock { Text = "Forbidden operations", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(CreateHintTextBlock("Use a ForbiddenOperation with one or more OperationMatcher children. A matcher can narrow by kind, staticAccess, ContainingType, and Member.", new Thickness(0, 0, 0, 2)));
        panel.Children.Add(ruleXml);

        AutoSaveOnLostFocus(description, Save, canEdit);
        AutoSaveOnLostFocus(ruleXml, Save, canEdit);

        var remove = CreateDangerButton("Remove forbidden operation policy", canEdit);
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

    private UIElement CreateNewForbiddenOperationPolicyEditor(ArchitectureLayerEditHandle handle)
    {
        var expander = new Expander
        {
            Header = "Add forbidden operation policy",
            IsExpanded = false,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel();
        var ruleXml = CreateForbiddenOperationRuleXmlBox(
            """
			<ForbiddenOperation allowedSites="StaticMember">
			  <OperationMatcher kind="PropertyRead" staticAccess="true">
			    <ContainingType exactFullName="System.DateTime" />
			    <Member exactName="UtcNow" memberKind="Property" />
			  </OperationMatcher>
			</ForbiddenOperation>
			""",
            handle.CanEdit);
        var description = CreateDescriptionBox(null, handle.CanEdit);

        panel.Children.Add(new TextBlock { Text = "Forbidden operations", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(ruleXml);
        panel.Children.Add(CreateHintTextBlock("The starter blocks direct DateTime.UtcNow access. Replace it with the exact member and sites your layer should avoid.", new Thickness(0, 2, 0, 0)));
        panel.Children.Add(new TextBlock { Text = "Policy description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);

        var add = new Button { Content = "Add forbidden operation policy", IsEnabled = handle.CanEdit, Margin = new Thickness(0, 8, 0, 0) };
        add.Click += (_, _) =>
        {
            var result = _editService.AddForbiddenOperationPolicy(handle, CreateForbiddenOperationPolicyAttributes(description.Text), ruleXml.Text);
            HandleEditResult(result, true);
        };
        panel.Children.Add(add);
        expander.Content = panel;

        return expander;
    }

    private static TextBox CreateForbiddenOperationRuleXmlBox(string text, bool isEnabled)
    {
        var result = new TextBox
        {
            Text = text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 148,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsEnabled = isEnabled
        };

        return result;
    }

    private static ImmutableDictionary<string, string> CreateForbiddenOperationPolicyAttributes(string? description)
    {
        var attributes = ImmutableDictionary<string, string>.Empty;
        if (description is { Length: > 0 } && !string.IsNullOrWhiteSpace(description))
        {
            attributes = attributes.Add("description", description);
        }

        return attributes;
    }
}