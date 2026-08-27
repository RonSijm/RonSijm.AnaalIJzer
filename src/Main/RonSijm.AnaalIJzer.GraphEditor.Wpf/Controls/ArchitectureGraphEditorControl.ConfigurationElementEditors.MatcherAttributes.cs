using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private static MatcherAttributeEditor CreateMatcherAttributeEditor(ComboBox elementKind, bool isEnabled)
	{
		var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
		panel.Children.Add(new TextBlock { Text = "Matcher attribute", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2) });
		var row = new Grid();
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		var attributeName = new ComboBox { IsEnabled = isEnabled };
		Grid.SetColumn(attributeName, 0);
		row.Children.Add(attributeName);
		var attributeValue = new TextBox { TextWrapping = TextWrapping.Wrap, IsEnabled = isEnabled };
		Grid.SetColumn(attributeValue, 2);
		row.Children.Add(attributeValue);
		panel.Children.Add(row);
		var editor = new MatcherAttributeEditor(panel, attributeName, attributeValue);
		void UpdateAttributes()
		{
			editor.SetAttributeNames(GetMatcherAttributeNames(elementKind.SelectedItem as string));
		}

		elementKind.SelectionChanged += (_, _) => UpdateAttributes();
		UpdateAttributes();

		return editor;
	}

	private static ImmutableArray<string> GetMatcherAttributeNames(string? elementKind)
	{
		var result = MatcherAttributeOptions.GetNames(elementKind);

		return result;
	}
}
