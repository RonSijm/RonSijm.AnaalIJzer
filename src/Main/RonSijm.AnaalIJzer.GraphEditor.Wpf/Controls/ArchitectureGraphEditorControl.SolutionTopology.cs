using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddSolutionTopologyOverview(StackPanel panel)
	{
		panel.Children.Add(CreateSectionTitle("Solution topology"));
		panel.Children.Add(CreateHintTextBlock("Module nodes and module-reference edges are read-only in this graph. They describe solution-wide project structure, not type layers.", new Thickness(0, 2, 0, 8)));
	}

	private UIElement CreateSolutionTopologyModuleInspector(ArchitectureGraphSelection selection)
	{
		var panel = CreateInspectorShell(selection);
		panel.Children.Add(CreateSectionTitle("Solution module"));
		panel.Children.Add(CreateHintTextBlock("Solution topology is intentionally read-only in the layer graph editor. Edit the <SolutionTopology> XML to change module matching.", new Thickness(0, 2, 0, 8)));
		var moduleName = selection.Title.StartsWith("Solution module ", StringComparison.Ordinal)
			? selection.Title.Substring("Solution module ".Length)
			: selection.Title;
		AddReadOnlyRow(panel, "Module", moduleName);
		AddReadOnlyRow(panel, "Path", selection.RelatedLayerPath);
		if (!string.IsNullOrWhiteSpace(selection.EvidenceDetails))
		{
			panel.Children.Add(CreateSectionTitle("Matching"));
			panel.Children.Add(new TextBlock { Text = selection.EvidenceDetails, TextWrapping = TextWrapping.Wrap });
		}

		return panel;
	}

	private UIElement CreateSolutionTopologyRuleInspector(ArchitectureGraphSelection selection)
	{
		var handle = selection.DependencyHandle;
		var panel = CreateInspectorShell(selection);
		panel.Children.Add(CreateSectionTitle("Solution topology rule"));
		panel.Children.Add(CreateHintTextBlock("This graph shows solution-module relationships. Edit the <SolutionTopology> XML to change this rule.", new Thickness(0, 2, 0, 8)));
		AddReadOnlyRow(panel, "Rule", handle.ElementKind);
		AddReadOnlyRow(panel, "From", handle.ConfiguredFrom);
		AddReadOnlyRow(panel, "To", handle.ConfiguredTo);
		AddReadOnlyRow(panel, "Source", string.IsNullOrWhiteSpace(handle.SourcePath) ? "Not available" : handle.SourcePath);
		if (!string.IsNullOrWhiteSpace(selection.EvidenceDetails))
		{
			panel.Children.Add(CreateSectionTitle("Description"));
			panel.Children.Add(new TextBlock { Text = selection.EvidenceDetails, TextWrapping = TextWrapping.Wrap });
		}

		return panel;
	}
}
