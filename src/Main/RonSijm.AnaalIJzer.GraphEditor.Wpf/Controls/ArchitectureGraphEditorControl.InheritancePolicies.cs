using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddInheritancePolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("Inheritance policies"));
		panel.Children.Add(CreateHintTextBlock("Require types in this layer to inherit base types or implement interfaces before they count as valid declarations here.", new Thickness(0, 0, 0, 4)));
		if (policies.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var policy in policies)
		{
			panel.Children.Add(CreateInheritancePolicyEditor(policy));
		}

		panel.Children.Add(CreateNewInheritancePolicyEditor(handle));
	}

	private UIElement CreateInheritancePolicyEditor(ArchitectureConfigurationElementDetails policy)
	{
		var expander = new Expander
		{
			Header = policy.Summary,
			IsExpanded = false,
			Margin = new Thickness(0, 4, 0, 0)
		};
		var panel = new StackPanel();
		var canEdit = policy.Handle.CanEdit;
		var typeKinds = new TextBox { Text = policy.Attributes.TryGetValue("typeKinds", out var configuredTypeKinds) ? configuredTypeKinds : string.Empty, IsEnabled = canEdit };
		var requiredBaseTypes = new TextBox { Text = policy.Attributes.TryGetValue("requiredBaseTypes", out var configuredBaseTypes) ? configuredBaseTypes : string.Empty, IsEnabled = canEdit };
		var requiredInterfaces = new TextBox { Text = policy.Attributes.TryGetValue("requiredInterfaces", out var configuredInterfaces) ? configuredInterfaces : string.Empty, IsEnabled = canEdit };
		var description = CreateDescriptionBox(policy.Attributes.TryGetValue("description", out var configuredDescription) ? configuredDescription : null, canEdit);

		panel.Children.Add(new TextBlock { Text = "Type kinds", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2) });
		panel.Children.Add(typeKinds);
		panel.Children.Add(CreateHintTextBlock("Comma-separated, for example Class or Class, Record.", new Thickness(0, 2, 0, 0)));
		panel.Children.Add(new TextBlock { Text = "Required base types", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(requiredBaseTypes);
		panel.Children.Add(CreateHintTextBlock("Comma-separated simple or full type names. Optional when requiredInterfaces is set.", new Thickness(0, 2, 0, 0)));
		panel.Children.Add(new TextBlock { Text = "Required interfaces", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(requiredInterfaces);
		panel.Children.Add(CreateHintTextBlock("Comma-separated simple or full interface names. Optional when requiredBaseTypes is set.", new Thickness(0, 2, 0, 0)));
		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);

		ArchitectureConfigurationEditResult Save()
		{
			if (string.IsNullOrWhiteSpace(typeKinds.Text)
			    || (string.IsNullOrWhiteSpace(requiredBaseTypes.Text) && string.IsNullOrWhiteSpace(requiredInterfaces.Text)))
			{
				return ArchitectureConfigurationEditResult.Failure("InheritancePolicy requires typeKinds and at least one of requiredBaseTypes or requiredInterfaces.");
			}

			var attributes = ImmutableDictionary<string, string>.Empty
				.Add("typeKinds", typeKinds.Text)
				.Add("description", description.Text ?? string.Empty);

			if (!string.IsNullOrWhiteSpace(requiredBaseTypes.Text))
			{
				attributes = attributes.Add("requiredBaseTypes", requiredBaseTypes.Text);
			}

			if (!string.IsNullOrWhiteSpace(requiredInterfaces.Text))
			{
				attributes = attributes.Add("requiredInterfaces", requiredInterfaces.Text);
			}

			var result = _editService.SetConfigurationElementAttributes(policy.Handle, attributes);

			return result;
		}

		AutoSaveOnLostFocus(typeKinds, Save, canEdit);
		AutoSaveOnLostFocus(requiredBaseTypes, Save, canEdit);
		AutoSaveOnLostFocus(requiredInterfaces, Save, canEdit);
		AutoSaveOnLostFocus(description, Save, canEdit);

		var remove = CreateDangerButton("Remove inheritance policy", canEdit);
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

	private UIElement CreateNewInheritancePolicyEditor(ArchitectureLayerEditHandle handle)
	{
		var expander = new Expander
		{
			Header = "Add inheritance policy",
			IsExpanded = false,
			Margin = new Thickness(0, 8, 0, 0)
		};
		var panel = new StackPanel();
		var typeKinds = new TextBox { Text = "Class", IsEnabled = handle.CanEdit };
		var requiredBaseTypes = new TextBox { Text = "Entity", IsEnabled = handle.CanEdit };
		var requiredInterfaces = new TextBox { IsEnabled = handle.CanEdit };
		var description = CreateDescriptionBox(null, handle.CanEdit);

		panel.Children.Add(new TextBlock { Text = "Type kinds", FontWeight = FontWeights.SemiBold });
		panel.Children.Add(typeKinds);
		panel.Children.Add(new TextBlock { Text = "Required base types", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(requiredBaseTypes);
		panel.Children.Add(new TextBlock { Text = "Required interfaces", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(requiredInterfaces);
		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);

		var add = new Button { Content = "Add inheritance policy", Margin = new Thickness(0, 8, 0, 0), IsEnabled = handle.CanEdit };
		add.Click += (_, _) =>
		{
			if (string.IsNullOrWhiteSpace(typeKinds.Text)
			    || (string.IsNullOrWhiteSpace(requiredBaseTypes.Text) && string.IsNullOrWhiteSpace(requiredInterfaces.Text)))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure("InheritancePolicy requires typeKinds and at least one of requiredBaseTypes or requiredInterfaces."), false);
				return;
			}

			var attributes = ImmutableDictionary<string, string>.Empty
				.Add("typeKinds", typeKinds.Text)
				.Add("description", description.Text ?? string.Empty);

			if (!string.IsNullOrWhiteSpace(requiredBaseTypes.Text))
			{
				attributes = attributes.Add("requiredBaseTypes", requiredBaseTypes.Text);
			}

			if (!string.IsNullOrWhiteSpace(requiredInterfaces.Text))
			{
				attributes = attributes.Add("requiredInterfaces", requiredInterfaces.Text);
			}

			HandleEditResult(_editService.AddInheritancePolicy(handle, attributes), true);
		};
		panel.Children.Add(add);
		expander.Content = panel;

		return expander;
	}
}
