using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private static readonly ImmutableArray<string> VisibilityTargetNames =
	[
		"Type",
		"Constructor",
		"Method",
		"Property",
		"Field",
		"Event",
		"Operator",
		"Conversion",
		"NestedType"
	];

	private static readonly ImmutableArray<string> AccessibilityNames =
	[
		"Public",
		"Internal",
		"Protected",
		"ProtectedInternal",
		"PrivateProtected",
		"Private",
		"File"
	];

	private void AddVisibilityPolicyEditors(StackPanel panel, ImmutableArray<ArchitectureConfigurationElementDetails> policies, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("Visibility policies"));
		panel.Children.Add(CreateHintTextBlock("Restrict declared accessibility. Parent and child policies are cumulative.", new Thickness(0, 0, 0, 4)));
		if (policies.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var policy in policies)
		{
			panel.Children.Add(CreateVisibilityPolicyEditor(policy));
		}

		panel.Children.Add(CreateNewVisibilityPolicyEditor(handle));
	}

	private UIElement CreateVisibilityPolicyEditor(ArchitectureConfigurationElementDetails policy)
	{
		var expander = new Expander
		{
			Header = policy.Summary,
			IsExpanded = false,
			Margin = new Thickness(0, 4, 0, 0)
		};
		var panel = new StackPanel();
		var canEdit = policy.Handle.CanEdit;
		var isBlockList = policy.Attributes.ContainsKey("blockedAccessibilities");
		var mode = CreateVisibilityModeSelector(isBlockList, canEdit);
		var targetChecks = CreateOptionChecks(VisibilityTargetNames, ParseCommaSeparated(policy.Attributes, "targets"), canEdit);
		var configuredAccessibilities = ParseCommaSeparated(policy.Attributes, isBlockList ? "blockedAccessibilities" : "allowedAccessibilities");
		var accessibilityChecks = CreateOptionChecks(AccessibilityNames, configuredAccessibilities, canEdit);
		var description = CreateDescriptionBox(policy.Attributes.TryGetValue("description", out var configuredDescription) ? configuredDescription : null, canEdit);

		panel.Children.Add(new TextBlock { Text = "Mode", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2) });
		panel.Children.Add(mode);
		panel.Children.Add(new TextBlock { Text = "Targets", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(targetChecks.Panel);
		panel.Children.Add(new TextBlock { Text = "Accessibilities", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(accessibilityChecks.Panel);
		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);

		ArchitectureConfigurationEditResult Save()
		{
			var attributes = CreateVisibilityPolicyAttributes(mode, targetChecks.Checks, accessibilityChecks.Checks, description.Text);
			if (attributes is null)
			{
				return ArchitectureConfigurationEditResult.Failure("Select at least one target and one accessibility.");
			}

			var result = _editService.SetConfigurationElementAttributes(policy.Handle, attributes);

			return result;
		}

		AutoSaveOnSelectionChanged(mode, Save, canEdit);
		AutoSaveOnSiteChecks(targetChecks.Checks, Save, canEdit);
		AutoSaveOnSiteChecks(accessibilityChecks.Checks, Save, canEdit);
		AutoSaveOnLostFocus(description, Save, canEdit);

		var remove = CreateDangerButton("Remove visibility policy", canEdit);
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

	private UIElement CreateNewVisibilityPolicyEditor(ArchitectureLayerEditHandle handle)
	{
		var expander = new Expander
		{
			Header = "Add visibility policy",
			IsExpanded = false,
			Margin = new Thickness(0, 8, 0, 0)
		};
		var panel = new StackPanel();
		var mode = CreateVisibilityModeSelector(false, handle.CanEdit);
		var targetChecks = CreateOptionChecks(VisibilityTargetNames, ["Type"], handle.CanEdit);
		var accessibilityChecks = CreateOptionChecks(AccessibilityNames, ["Internal"], handle.CanEdit);
		var description = CreateDescriptionBox(null, handle.CanEdit);
		panel.Children.Add(new TextBlock { Text = "Mode", FontWeight = FontWeights.SemiBold });
		panel.Children.Add(mode);
		panel.Children.Add(new TextBlock { Text = "Targets", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(targetChecks.Panel);
		panel.Children.Add(new TextBlock { Text = "Accessibilities", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(accessibilityChecks.Panel);
		panel.Children.Add(new TextBlock { Text = "Description", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(description);
		var add = new Button { Content = "Add visibility policy", Margin = new Thickness(0, 8, 0, 0), IsEnabled = handle.CanEdit };
		add.Click += (_, _) =>
		{
			var attributes = CreateVisibilityPolicyAttributes(mode, targetChecks.Checks, accessibilityChecks.Checks, description.Text);
			var result = attributes is null
				? ArchitectureConfigurationEditResult.Failure("Select at least one target and one accessibility.")
				: _editService.AddVisibilityPolicy(handle, attributes);
			HandleEditResult(result, true);
		};
		panel.Children.Add(add);
		expander.Content = panel;

		return expander;
	}

}
