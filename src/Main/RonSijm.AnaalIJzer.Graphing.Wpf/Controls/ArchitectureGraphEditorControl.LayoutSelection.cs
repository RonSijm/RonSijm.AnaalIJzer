using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.Graphing.Model;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Layout;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void EnsureLayoutState(ArchitectureConfigurationSource source)
	{
		var sourceKey = ArchitectureGraphLayoutState.CreateSourceKey(source);
		if (string.Equals(layoutState.SourceKey, sourceKey, StringComparison.Ordinal))
		{
			return;
		}

		layoutState.Save();
		layoutState = ArchitectureGraphLayoutState.Load(source, warningLogger);
	}

	private ArchitectureGraphSelection RemapSelection(ArchitectureGraphSelection selection)
	{
		var result = selection.Kind switch
		{
			ArchitectureGraphSelectionKind.Layer => RemapLayerSelection(selection),
			ArchitectureGraphSelectionKind.DependencyRule => RemapDependencySelection(selection),
			_ => ArchitectureGraphSelection.None
		};

		return result;
	}

	private ArchitectureGraphSelection RemapLayerSelection(ArchitectureGraphSelection selection)
	{
		var layer = snapshot.Layers.FirstOrDefault(item => string.Equals(item.Path, selection.LayerHandle.LayerPath, StringComparison.Ordinal));
		var result = layer is null ? ArchitectureGraphSelection.None : ArchitectureGraphSelection.ForLayer(layer.EditHandle);

		return result;
	}

	private ArchitectureGraphSelection RemapDependencySelection(ArchitectureGraphSelection selection)
	{
		var handle = selection.DependencyHandle;
		var rule = snapshot.Rules.FirstOrDefault(item =>
			string.Equals(item.Kind, handle.ElementKind, StringComparison.Ordinal)
			&& string.Equals(item.ScopePath, handle.ScopePath, StringComparison.Ordinal)
			&& string.Equals(item.ConfiguredFrom, handle.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(item.ConfiguredTo, handle.ConfiguredTo, StringComparison.Ordinal)
			&& (handle.XmlLineNumber <= 0 || item.XmlLineNumber == handle.XmlLineNumber));
		rule ??= snapshot.Rules.FirstOrDefault(item =>
			string.Equals(item.Kind, handle.ElementKind, StringComparison.Ordinal)
			&& string.Equals(item.ScopePath, handle.ScopePath, StringComparison.Ordinal)
			&& string.Equals(item.ConfiguredFrom, handle.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(item.ConfiguredTo, handle.ConfiguredTo, StringComparison.Ordinal));
		var result = rule is null ? ArchitectureGraphSelection.None : ArchitectureGraphSelection.ForDependency(rule.EditHandle);

		return result;
	}

	private static void AddSection(StackPanel panel, string title, IReadOnlyCollection<string> rows)
	{
		if (rows.Count == 0)
		{
			return;
		}

		panel.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 6, 0, 2), FontWeight = FontWeights.SemiBold });
		foreach (var row in rows)
		{
			panel.Children.Add(new TextBlock { Text = row, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 1, 0, 1) });
		}
	}

	private static string FormatActiveLayers(ArchitectureGraphSnapshot snapshot)
	{
		var result = snapshot.ActiveLayerPaths.Length == 0 ? "none" : string.Join(", ", snapshot.ActiveLayerPaths);

		return result;
	}

	private static string CreateGroupKey(ArchitectureGraphGroupViewModel group)
	{
		var result = group.Title;

		return result;
	}
}
