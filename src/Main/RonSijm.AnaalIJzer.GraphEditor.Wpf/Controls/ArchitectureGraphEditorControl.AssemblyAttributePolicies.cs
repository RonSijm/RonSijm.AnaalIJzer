using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
    private void AddAssemblyAttributePolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureConfigurationSource source)
    {
        panel.Children.Add(CreateSectionTitle("Assembly attribute policies"));
        panel.Children.Add(CreateHintTextBlock("Control metadata emitted for this assembly, including handwritten [assembly: ...] declarations and SDK-generated attributes such as InternalsVisibleTo. These policies are source metadata, not dependency graph edges.", new Thickness(0, 0, 0, 4)));
        if (policies.Length == 0)
        {
            panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
        }

        foreach (var policy in policies)
        {
            panel.Children.Add(CreateAssemblyAttributePolicyEditor(policy));
        }

        panel.Children.Add(CreateNewAssemblyAttributePolicyEditor(source));
    }

    private UIElement CreateAssemblyAttributePolicyEditor(ArchitectureConfigurationElementDetails policy)
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
        var policyXml = CreateAssemblyAttributePolicyXmlBox(policy.ChildXml, canEdit);

        ArchitectureConfigurationEditResult Save()
        {
            if (string.IsNullOrWhiteSpace(policyXml.Text))
            {
                return ArchitectureConfigurationEditResult.Failure("AssemblyAttributePolicy requires at least one Allowed or Forbidden Attribute rule.");
            }

            var attributesResult = _editService.SetConfigurationElementAttributes(policy.Handle, CreateAssemblyAttributePolicyAttributes(description.Text));
            if (!attributesResult.Succeeded)
            {
                return attributesResult;
            }

            var result = _editService.SetConfigurationElementChildren(policy.Handle, policyXml.Text);

            return result;
        }

        panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);
        panel.Children.Add(new TextBlock { Text = "Allowed and forbidden attributes", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(CreateHintTextBlock("Use one or more <Attribute> rules inside <Allowed> or <Forbidden>. An Attribute can contain positional or named <Argument> matchers. Every Argument in one Attribute rule must match.", new Thickness(0, 0, 0, 2)));
        panel.Children.Add(policyXml);

        AutoSaveOnLostFocus(description, Save, canEdit);
        AutoSaveOnLostFocus(policyXml, Save, canEdit);

        var remove = CreateDangerButton("Remove assembly attribute policy", canEdit);
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

    private UIElement CreateNewAssemblyAttributePolicyEditor(ArchitectureConfigurationSource source)
    {
        var expander = new Expander
        {
            Header = "Add assembly attribute policy",
            IsExpanded = false,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel();
        var policyXml = CreateAssemblyAttributePolicyXmlBox(
            """
			<Forbidden>
			  <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
			    <Argument index="0" exactName="UnapprovedAssembly" />
			  </Attribute>
			</Forbidden>
			""",
            source.CanEdit);
        var description = CreateDescriptionBox(null, source.CanEdit);

        panel.Children.Add(new TextBlock { Text = "Allowed and forbidden attributes", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(policyXml);
        panel.Children.Add(CreateHintTextBlock("The starter blocks one InternalsVisibleTo friend. Replace the attribute type and argument matchers with the metadata policy your team owns.", new Thickness(0, 2, 0, 0)));
        panel.Children.Add(new TextBlock { Text = "Policy description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);

        var add = new Button { Content = "Add assembly attribute policy", IsEnabled = source.CanEdit, Margin = new Thickness(0, 8, 0, 0) };
        add.Click += (_, _) =>
        {
            var result = _editService.AddAssemblyAttributePolicy(source, CreateAssemblyAttributePolicyAttributes(description.Text), policyXml.Text);
            HandleEditResult(result, true);
        };
        panel.Children.Add(add);
        expander.Content = panel;

        return expander;
    }

    private static TextBox CreateAssemblyAttributePolicyXmlBox(string text, bool isEnabled)
    {
        var result = new TextBox
        {
            Text = text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 176,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsEnabled = isEnabled
        };

        return result;
    }

    private static ImmutableDictionary<string, string> CreateAssemblyAttributePolicyAttributes(string? description)
    {
        var attributes = ImmutableDictionary<string, string>.Empty;
        if (description is { Length: > 0 } && !string.IsNullOrWhiteSpace(description))
        {
            attributes = attributes.Add("description", description);
        }

        return attributes;
    }
}