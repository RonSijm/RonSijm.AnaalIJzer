using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
    private void AddOperationContractEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> contracts, ArchitectureConfigurationSource source)
    {
        panel.Children.Add(CreateSectionTitle("Operation contracts"));
        panel.Children.Add(CreateHintTextBlock("Explicitly connect selected owner methods, optional request and response types, and entry points. These are source contracts, not dependency graph edges.", new Thickness(0, 0, 0, 4)));
        if (contracts.Length == 0)
        {
            panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
        }

        foreach (var contract in contracts)
        {
            panel.Children.Add(CreateOperationContractEditor(contract));
        }

        panel.Children.Add(CreateNewOperationContractEditor(source));
    }

    private UIElement CreateOperationContractEditor(ArchitectureConfigurationElementDetails contract)
    {
        var expander = new Expander
        {
            Header = contract.Summary,
            IsExpanded = false,
            Margin = new Thickness(0, 4, 0, 0)
        };
        var panel = new StackPanel();
        var canEdit = contract.Handle.CanEdit;
        var description = CreateDescriptionBox(contract.Attributes.TryGetValue("description", out var configuredDescription) ? configuredDescription : null, canEdit);
        var operationXml = CreateOperationContractXmlBox(contract.ChildXml, canEdit);

        ArchitectureConfigurationEditResult Save()
        {
            if (string.IsNullOrWhiteSpace(operationXml.Text))
            {
                return ArchitectureConfigurationEditResult.Failure("Operations requires at least one Operation child.");
            }

            var attributesResult = _editService.SetConfigurationElementAttributes(contract.Handle, CreateOperationContractAttributes(description.Text));
            if (!attributesResult.Succeeded)
            {
                return attributesResult;
            }

            var result = _editService.SetConfigurationElementChildren(contract.Handle, operationXml.Text);

            return result;
        }

        panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);
        panel.Children.Add(new TextBlock { Text = "Operations", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(CreateHintTextBlock("Each Operation requires one Owner declaration matcher. Request, Response, and EntryPoint selectors are optional and use the shared matcher vocabulary.", new Thickness(0, 0, 0, 2)));
        panel.Children.Add(operationXml);

        AutoSaveOnLostFocus(description, Save, canEdit);
        AutoSaveOnLostFocus(operationXml, Save, canEdit);

        var remove = CreateDangerButton("Remove operation contracts", canEdit);
        remove.Margin = new Thickness(0, 8, 0, 0);
        remove.Click += (_, _) =>
        {
            if (_confirmationHandler("Remove '" + contract.Summary + "'?"))
            {
                HandleEditResult(_editService.RemoveConfigurationElement(contract.Handle), true);
            }
        };
        panel.Children.Add(remove);
        expander.Content = panel;

        return expander;
    }

    private UIElement CreateNewOperationContractEditor(ArchitectureConfigurationSource source)
    {
        var expander = new Expander
        {
            Header = "Add operation contracts",
            IsExpanded = false,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var panel = new StackPanel();
        var operationXml = CreateOperationContractXmlBox(
            """
			<Operation name="PlacePizzaOrder" allowedOwnerLayers="Application" allowedEntryPointLayers="Controller">
			  <Owner>
			    <DeclarationMatcher>
			      <ContainingType endsWith="Kitchen" />
			      <Member exactName="PlacePizzaOrder" memberKind="Method" />
			    </DeclarationMatcher>
			  </Owner>
			  <Request><Class exactName="PlacePizzaOrderRequest" /></Request>
			  <Response><Class exactName="PlacePizzaOrderResponse" /></Response>
			  <EntryPoint>
			    <DeclarationMatcher>
			      <ContainingType endsWith="Controller" />
			      <Member exactName="PlacePizzaOrder" memberKind="Method" />
			    </DeclarationMatcher>
			  </EntryPoint>
			</Operation>
			""",
            source.CanEdit);
        var description = CreateDescriptionBox(null, source.CanEdit);

        panel.Children.Add(new TextBlock { Text = "Operations", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(operationXml);
        panel.Children.Add(CreateHintTextBlock("The starter maps one controller entry point to one kitchen owner. Replace every selector with your explicit operation contract.", new Thickness(0, 2, 0, 0)));
        panel.Children.Add(new TextBlock { Text = "Container description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
        panel.Children.Add(description);

        var add = new Button { Content = "Add operation contracts", IsEnabled = source.CanEdit, Margin = new Thickness(0, 8, 0, 0) };
        add.Click += (_, _) =>
        {
            var result = _editService.AddOperationContracts(source, CreateOperationContractAttributes(description.Text), operationXml.Text);
            HandleEditResult(result, true);
        };
        panel.Children.Add(add);
        expander.Content = panel;

        return expander;
    }

    private static TextBox CreateOperationContractXmlBox(string text, bool isEnabled)
    {
        var result = new TextBox
        {
            Text = text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 272,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsEnabled = isEnabled
        };

        return result;
    }

    private static ImmutableDictionary<string, string> CreateOperationContractAttributes(string? description)
    {
        var attributes = ImmutableDictionary<string, string>.Empty;
        if (description is { Length: > 0 } && !string.IsNullOrWhiteSpace(description))
        {
            attributes = attributes.Add("description", description);
        }

        return attributes;
    }
}