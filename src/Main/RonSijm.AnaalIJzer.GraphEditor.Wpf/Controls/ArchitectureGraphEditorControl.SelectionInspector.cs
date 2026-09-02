using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void RenderSelection(ArchitectureGraphSelection selection)
	{
		_currentSelection = selection;
		_inspectorPanel.Child = selection.Kind switch
		{
			ArchitectureGraphSelectionKind.Layer => CreateLayerInspector(selection),
			ArchitectureGraphSelectionKind.DependencyRule => CreateDependencyRuleInspector(selection),
			ArchitectureGraphSelectionKind.CodeEvidence => CreateCodeEvidenceInspector(selection),
			_ => CreateEmptyInspector(selection)
		};
		_inspectorScrollViewer.ScrollToTop();
	}

	private UIElement CreateEmptyInspector(ArchitectureGraphSelection selection)
	{
		var panel = CreateInspectorShell(selection);
		panel.Children.Add(CreateHintTextBlock("Click a layer node or dependency connection to edit its settings.", new Thickness(0, 8, 0, 0)));
		if (_snapshot.HasConfiguration && !_snapshot.HasConfigurationIssues)
		{
			AddRootConfigurationEditor(panel, _snapshot.ConfigurationSource);
		}

		return panel;
	}

	private UIElement CreateCodeEvidenceInspector(ArchitectureGraphSelection selection)
	{
		var panel = CreateInspectorShell(selection);
		panel.Children.Add(CreateSectionTitle("Observed code"));
		panel.Children.Add(CreateHintTextBlock("This connection comes from project code evidence. Edit code or add dependency rules to change it.", new Thickness(0, 2, 0, 6)));
		AddConfigurationFixesEditor(panel, selection);
		if (!string.IsNullOrWhiteSpace(selection.EvidenceDetails))
		{
			panel.Children.Add(new TextBlock { Text = selection.EvidenceDetails, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Consolas") });
		}

		return panel;
	}
}
