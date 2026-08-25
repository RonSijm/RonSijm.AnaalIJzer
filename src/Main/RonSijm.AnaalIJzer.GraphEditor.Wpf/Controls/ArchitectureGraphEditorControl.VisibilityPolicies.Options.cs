using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private static ComboBox CreateVisibilityModeSelector(bool isBlockList, bool canEdit)
	{
		var result = new ComboBox { IsEnabled = canEdit };
		result.Items.Add("Allow only");
		result.Items.Add("Block");
		result.SelectedIndex = isBlockList ? 1 : 0;

		return result;
	}

	private static (WrapPanel Panel, ImmutableArray<CheckBox> Checks) CreateOptionChecks(ImmutableArray<string> options, ImmutableArray<string> selected, bool canEdit)
	{
		var panel = new WrapPanel();
		var checks = ImmutableArray.CreateBuilder<CheckBox>();
		foreach (var option in options)
		{
			var check = new CheckBox
			{
				Content = option,
				IsChecked = selected.Contains(option, StringComparer.OrdinalIgnoreCase),
				IsEnabled = canEdit,
				Margin = new Thickness(0, 0, 8, 4)
			};
			panel.Children.Add(check);
			checks.Add(check);
		}

		return (panel, checks.ToImmutable());
	}

	private static ImmutableArray<string> ParseCommaSeparated(ImmutableDictionary<string, string> attributes, string key)
	{
		if (!attributes.TryGetValue(key, out var value))
		{
			return ImmutableArray<string>.Empty;
		}

		var result = value.Split(',').Select(item => item.Trim()).Where(item => item.Length > 0).ToImmutableArray();

		return result;
	}

	private static ImmutableDictionary<string, string>? CreateVisibilityPolicyAttributes(ComboBox mode, ImmutableArray<CheckBox> targetChecks, ImmutableArray<CheckBox> accessibilityChecks, string? description)
	{
		var targets = GetCheckedValues(targetChecks);
		var accessibilities = GetCheckedValues(accessibilityChecks);
		if (targets.Length == 0 || accessibilities.Length == 0)
		{
			return null;
		}

		var accessibilitiesAttribute = mode.SelectedIndex == 1 ? "blockedAccessibilities" : "allowedAccessibilities";
		var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		builder["targets"] = string.Join(", ", targets);
		builder[accessibilitiesAttribute] = string.Join(", ", accessibilities);
		if (!string.IsNullOrWhiteSpace(description))
		{
			builder["description"] = description!.Trim();
		}

		return builder.ToImmutable();
	}

	private static ImmutableArray<string> GetCheckedValues(ImmutableArray<CheckBox> checks)
	{
		var result = checks
			.Where(check => check.IsChecked == true)
			.Select(check => check.Content?.ToString() ?? string.Empty)
			.Where(value => value.Length > 0)
			.ToImmutableArray();

		return result;
	}
}
