using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
    private void AddBehavioralOperationPolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureLayerEditHandle handle)
    {
        panel.Children.Add(CreateSectionTitle("Behavioral operation policies"));
        panel.Children.Add(CreateHintTextBlock("Require, order, forbid-after, or count selected resolved operations within explicitly selected declaration bodies. These policies prove limited source facts; they are not runtime behavior tests.", new Thickness(0, 0, 0, 4)));
        if (policies.Length == 0)
        {
            panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
        }

        foreach (var policy in policies)
        {
            panel.Children.Add(CreateBehavioralOperationPolicyEditor(policy));
        }

        panel.Children.Add(CreateNewBehavioralOperationPolicyEditor(handle));
    }

    private UIElement CreateBehavioralOperationPolicyEditor(ArchitectureConfigurationElementDetails policy)
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
        var ruleXml = CreateBehavioralOperationRuleXmlBox(policy.ChildXml, canEdit);

        ArchitectureConfigurationEditResult Save()
        {
            if (string.IsNullOrWhiteSpace(ruleXml.Text))
            {
                return ArchitectureConfigurationEditResult.Failure("BehavioralOperations requires at least one rule.");
            }

            var attributesResult = _editService.SetConfigurationElementAttributes(policy.Handle, CreateBehavioralOperationPolicyAttributes(description.Text));
            if (!attributesResult.Succeeded)
            {
                return attributesResult;
            }

            var result = _editService.SetConfigurationElementChildren(policy.Handle, ruleXml.Text);

            return result;
        }

        panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);
        panel.Children.Add(new TextBlock { Text = "Behavioral rules", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(CreateHintTextBlock("Each rule selects an owning declaration, then selected operations. RequiredOperationBefore and ForbiddenOperationAfter add a BeforeOperation or AfterOperation target. Ordering is Dominance by default, or Lexical when source order is intended.", new Thickness(0, 0, 0, 2)));
        panel.Children.Add(ruleXml);

        AutoSaveOnLostFocus(description, Save, canEdit);
        AutoSaveOnLostFocus(ruleXml, Save, canEdit);

        var remove = CreateDangerButton("Remove behavioral operation policy", canEdit);
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

    private UIElement CreateNewBehavioralOperationPolicyEditor(ArchitectureLayerEditHandle handle)
    {
        var expander = new Expander
        {
            Header = "Add behavioral operation policy",
            IsExpanded = false,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel();
        var ruleXml = CreateBehavioralOperationRuleXmlBox(
            """
			<RequiredOperationBefore>
			  <DeclarationMatcher>
			    <Member exactName="Submit" memberKind="Method" />
			  </DeclarationMatcher>
			  <OperationMatcher kind="Invocation">
			    <ContainingType exactName="PizzaValidator" />
			    <Member exactName="Validate" memberKind="Method" />
			  </OperationMatcher>
			  <BeforeOperation>
			    <OperationMatcher kind="Invocation">
			      <ContainingType exactName="PizzaRepository" />
			      <Member exactName="Save" memberKind="Method" />
			    </OperationMatcher>
			  </BeforeOperation>
			</RequiredOperationBefore>
			""",
            handle.CanEdit);
        var description = CreateDescriptionBox(null, handle.CanEdit);

        panel.Children.Add(new TextBlock { Text = "Behavioral rules", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(ruleXml);
        panel.Children.Add(CreateHintTextBlock("The starter requires PizzaValidator.Validate before PizzaRepository.Save in Submit. Replace it with the precise source fact your layer needs to enforce.", new Thickness(0, 2, 0, 0)));
        panel.Children.Add(new TextBlock { Text = "Policy description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);

        var add = new Button { Content = "Add behavioral operation policy", IsEnabled = handle.CanEdit, Margin = new Thickness(0, 8, 0, 0) };
        add.Click += (_, _) =>
        {
            var result = _editService.AddBehavioralOperationPolicy(handle, CreateBehavioralOperationPolicyAttributes(description.Text), ruleXml.Text);
            HandleEditResult(result, true);
        };
        panel.Children.Add(add);
        expander.Content = panel;

        return expander;
    }

    private static TextBox CreateBehavioralOperationRuleXmlBox(string text, bool isEnabled)
    {
        var result = new TextBox
        {
            Text = text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 208,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsEnabled = isEnabled
        };

        return result;
    }

    private static ImmutableDictionary<string, string> CreateBehavioralOperationPolicyAttributes(string? description)
    {
        var attributes = ImmutableDictionary<string, string>.Empty;
        if (description is { Length: > 0 } && !string.IsNullOrWhiteSpace(description))
        {
            attributes = attributes.Add("description", description);
        }

        return attributes;
    }
}