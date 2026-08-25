using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private ApiSurfaceRuleEditor CreateApiSurfaceRuleEditor(XElement rule, bool canEdit, Panel owner)
	{
		var mode = new ComboBox { IsEnabled = canEdit };
		mode.Items.Add("Allow");
		mode.Items.Add("Block");
		mode.SelectedIndex = rule.Name.LocalName == "BlockedLayer" ? 1 : 0;
		var path = new TextBox { Text = rule.Attribute("path")?.Value ?? string.Empty, IsEnabled = canEdit };
		var siteMode = new ComboBox { IsEnabled = canEdit };
		siteMode.Items.Add("All sites");
		siteMode.Items.Add("Only selected sites");
		siteMode.Items.Add("All except selected sites");
		siteMode.SelectedIndex = rule.Attribute("allowedSites") is not null ? 1 : rule.Attribute("blockedSites") is not null ? 2 : 0;
		var selectedSites = (rule.Attribute("allowedSites")?.Value ?? rule.Attribute("blockedSites")?.Value ?? string.Empty)
			.Split(',')
			.Select(value => value.Trim())
			.Where(value => value.Length > 0)
			.ToImmutableArray();
		var siteChecks = CreateOptionChecks(ApiSurfaceSiteNames, selectedSites, canEdit);
		var description = CreateDescriptionBox(rule.Attribute("description")?.Value, canEdit);
		var remove = CreateDangerButton("Remove rule", canEdit);
		var root = new StackPanel { Margin = new Thickness(8, 4, 0, 6) };
		root.Children.Add(mode);
		root.Children.Add(path);
		root.Children.Add(siteMode);
		root.Children.Add(siteChecks.Panel);
		root.Children.Add(description);
		root.Children.Add(remove);
		var result = new ApiSurfaceRuleEditor(this, root, mode, path, siteMode, siteChecks.Checks, description, remove, canEdit);

		return result;
	}

	private sealed class ApiSurfaceRuleEditor(
		ArchitectureGraphEditorControl owner,
		StackPanel root,
		ComboBox mode,
		TextBox path,
		ComboBox siteMode,
		ImmutableArray<CheckBox> siteChecks,
		TextBox description,
		Button removeButton,
		bool canEdit)
	{
		internal StackPanel Root { get; } = root;
		internal Button RemoveButton { get; } = removeButton;

		internal XElement CreateElement()
		{
			var name = mode.SelectedIndex == 1 ? "BlockedLayer" : "AllowedLayer";
			var result = new XElement(name, new XAttribute("path", path.Text.Trim()));
			var selectedSites = GetCheckedValues(siteChecks);
			if (siteMode.SelectedIndex == 1 && selectedSites.Length > 0)
			{
				result.Add(new XAttribute("allowedSites", string.Join(", ", selectedSites)));
			}
			else if (siteMode.SelectedIndex == 2 && selectedSites.Length > 0)
			{
				result.Add(new XAttribute("blockedSites", string.Join(", ", selectedSites)));
			}
			if (!string.IsNullOrWhiteSpace(description.Text))
			{
				result.Add(new XAttribute("description", description.Text.Trim()));
			}

			return result;
		}

		internal void AttachAutoSave(Func<ArchitectureConfigurationEditResult> save)
		{
			owner.AutoSaveOnSelectionChanged(mode, save, canEdit);
			owner.AutoSaveOnSelectionChanged(siteMode, save, canEdit);
			owner.AutoSaveOnSiteChecks(siteChecks, save, canEdit);
			owner.AutoSaveOnLostFocus(path, save, canEdit);
			owner.AutoSaveOnLostFocus(description, save, canEdit);
		}
	}
}
