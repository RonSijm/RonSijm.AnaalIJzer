using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private UIElement CreateLayerInspector(ArchitectureGraphSelection selection)
	{
		var handle = selection.LayerHandle;
		var panel = CreateInspectorShell(selection);
		var details = ArchitectureConfigurationEditService.GetLayerDetails(handle);
		if (!details.Succeeded)
		{
			panel.Children.Add(new TextBlock { Text = details.Message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), Foreground = Brushes.IndianRed });
			return panel;
		}

		AddReadOnlyRow(panel, "Path", handle.LayerPath);
		AddReadOnlyRow(panel, "Source", handle.CanEdit ? handle.SourcePath : "Not editable");
		AddLayerCodeEvidence(panel, handle.LayerPath);
		panel.Children.Add(CreateSectionTitle("Name"));
		var name = new TextBox { Text = details.Name, IsEnabled = handle.CanEdit };
		panel.Children.Add(name);
		AutoSaveOnLostFocus(name, () => ArchitectureConfigurationEditService.SetLayerName(handle, name.Text), handle.CanEdit, true);
		AddLayerStructureEditor(panel, handle);
		var description = CreateDescriptionBox(details.Description, handle.CanEdit);
		panel.Children.Add(CreateSectionTitle("Description"));
		panel.Children.Add(description);
		AutoSaveOnLostFocus(description, () => ArchitectureConfigurationEditService.SetLayerDescription(handle, description.Text), handle.CanEdit);
		panel.Children.Add(CreateSectionTitle("Recognized dependencies"));
		var requiredSites = new TextBox { Text = details.RequireRecognizedDependencies ?? string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = handle.CanEdit };
		panel.Children.Add(requiredSites);
		AutoSaveOnLostFocus(requiredSites, () => ArchitectureConfigurationEditService.SetLayerRequireRecognizedDependencies(handle, requiredSites.Text), handle.CanEdit);
		AddConfigurationElementEditors(panel, "Layer matchers", details.Matchers, handle, "LayerMatcher", ImmutableArray.Create("Class", "Namespace", "Assembly"));
		AddConfigurationElementEditors(panel, "Allowed type policy", details.AllowedPolicies, handle, "Allowed", ImmutableArray.Create("Class", "Namespace"));
		AddConfigurationElementEditors(panel, "Forbidden type policy", details.ForbiddenPolicies, handle, "Forbidden", ImmutableArray.Create("Class", "Namespace"));

		return panel;
	}

	private void AddLayerCodeEvidence(StackPanel panel, string layerPath)
	{
		if (!snapshot.Evidence.HasEvidence || string.IsNullOrWhiteSpace(layerPath))
		{
			return;
		}

		var types = snapshot.Evidence.Types
			.Where(type => LayerEvidenceMatches(type.LayerPath, layerPath))
			.OrderBy(type => type.FullTypeName, StringComparer.Ordinal)
			.ToImmutableArray();
		var outgoing = snapshot.Evidence.Dependencies
			.Where(dependency => LayerEvidenceMatches(dependency.CallerLayerPath, layerPath))
			.OrderByDescending(dependency => dependency.IsViolation)
			.ThenBy(dependency => dependency.DependencyLayerPath, StringComparer.Ordinal)
			.ThenBy(dependency => dependency.CallerTypeName, StringComparer.Ordinal)
			.ToImmutableArray();
		var incoming = snapshot.Evidence.Dependencies
			.Where(dependency => LayerEvidenceMatches(dependency.DependencyLayerPath, layerPath))
			.OrderByDescending(dependency => dependency.IsViolation)
			.ThenBy(dependency => dependency.CallerLayerPath, StringComparer.Ordinal)
			.ThenBy(dependency => dependency.CallerTypeName, StringComparer.Ordinal)
			.ToImmutableArray();
		if (types.Length == 0 && outgoing.Length == 0 && incoming.Length == 0)
		{
			return;
		}

		var violationCount = outgoing.Count(dependency => dependency.IsViolation) + incoming.Count(dependency => dependency.IsViolation);
		panel.Children.Add(CreateSectionTitle("Code evidence"));
		panel.Children.Add(CreateHintTextBlock(types.Length + " type(s), " + outgoing.Length + " outgoing observation(s), " + incoming.Length + " incoming observation(s), " + violationCount + " violation observation(s).", new Thickness(0, 2, 0, 6)));
		AddEvidenceText(panel, "Types", types.Select(FormatTypeEvidence).Take(16).ToImmutableArray(), types.Length);
		AddEvidenceText(panel, "Outgoing", outgoing.Select(FormatDependencyEvidence).Take(16).ToImmutableArray(), outgoing.Length);
		AddEvidenceText(panel, "Incoming", incoming.Select(FormatDependencyEvidence).Take(16).ToImmutableArray(), incoming.Length);
	}

	private static void AddEvidenceText(StackPanel panel, string title, ImmutableArray<string> lines, int totalCount)
	{
		if (totalCount == 0)
		{
			return;
		}

		panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
		var omitted = totalCount > lines.Length ? Environment.NewLine + "... " + (totalCount - lines.Length) + " more" : string.Empty;
		panel.Children.Add(new TextBlock
		{
			Text = string.Join(Environment.NewLine, lines) + omitted,
			TextWrapping = TextWrapping.Wrap,
			FontFamily = new FontFamily("Consolas")
		});
	}

	private static string FormatTypeEvidence(ArchitectureGraphTypeEvidence evidence)
	{
		var result = evidence.FullTypeName + FormatLocation(evidence.FilePath, evidence.LineNumber);

		return result;
	}

	private static string FormatDependencyEvidence(ArchitectureGraphDependencyEvidence evidence)
	{
		var prefix = evidence.IsViolation ? "[!] " : string.Empty;
		var result = prefix
		             + evidence.CallerTypeName
		             + " ("
		             + evidence.CallerLayerPath
		             + ") -> "
		             + evidence.DependencyTypeName
		             + " ("
		             + evidence.DependencyLayerPath
		             + ") ["
		             + evidence.Site
		             + "] "
		             + evidence.Reason
		             + FormatLocation(evidence.FilePath, evidence.LineNumber);

		return result;
	}

	private static string FormatLocation(string filePath, int lineNumber)
	{
		if (string.IsNullOrWhiteSpace(filePath) || lineNumber <= 0)
		{
			return string.Empty;
		}

		var result = " (" + System.IO.Path.GetFileName(filePath) + ":" + lineNumber + ")";

		return result;
	}

	private static bool LayerEvidenceMatches(string candidateLayerPath, string layerPath)
	{
		var result = string.Equals(candidateLayerPath, layerPath, StringComparison.Ordinal)
		             || candidateLayerPath.StartsWith(layerPath + "/", StringComparison.Ordinal);

		return result;
	}
}
