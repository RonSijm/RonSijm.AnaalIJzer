using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;
using RonSijm.AnaalIJzer.Graphing.Wpf.Selection;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private StackPanel CreateInspectorShell(ArchitectureGraphSelection selection)
	{
		var panel = new StackPanel();
		panel.Children.Add(new TextBlock { Text = "Inspector", FontWeight = FontWeights.SemiBold, FontSize = 14 });
		panel.Children.Add(new TextBlock { Text = selection.Title, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0), FontWeight = FontWeights.SemiBold });
		panel.Children.Add(CreateHintTextBlock(selection.Subtitle, new Thickness(0, 2, 0, 6)));

		return panel;
	}

	private TextBlock CreateHintTextBlock(string text, Thickness margin)
	{
		var result = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = margin };
		theme.ApplyHintForeground(result);

		return result;
	}

	private static TextBlock CreateSectionTitle(string text)
	{
		var result = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) };

		return result;
	}

	private static TextBox CreateDescriptionBox(string? text, bool isEnabled)
	{
		var result = new TextBox { Text = text ?? string.Empty, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 64, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, IsEnabled = isEnabled };

		return result;
	}

	private static Button CreateDangerButton(string content, bool isEnabled)
	{
		var result = new Button { Content = content, Foreground = Brushes.IndianRed, IsEnabled = isEnabled };

		return result;
	}

	private static bool Confirm(string message)
	{
		var result = MessageBox.Show(message, "AnaalIJzer Graph Editor", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

		return result;
	}

	private static void AddReadOnlyRow(StackPanel panel, string label, string value)
	{
		panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
		panel.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Consolas") });
	}

	private static (WrapPanel Panel, ImmutableArray<CheckBox> Checks) CreateSiteChecks(ImmutableArray<string> selectedSites, bool isEnabled)
	{
		var panel = new WrapPanel();
		var checks = ImmutableArray.CreateBuilder<CheckBox>();
		foreach (var site in ArchitectureDependencySiteNames.All)
		{
			var check = new CheckBox { Content = site, IsChecked = selectedSites.Contains(site, StringComparer.Ordinal), Margin = new Thickness(0, 0, 8, 4), IsEnabled = isEnabled };
			checks.Add(check);
			panel.Children.Add(check);
		}

		var result = (panel, checks.ToImmutable());

		return result;
	}

	private static ImmutableArray<string> GetCheckedSites(ImmutableArray<CheckBox> checks)
	{
		var selected = checks
			.Where(check => check.IsChecked == true)
			.Select(check => check.Content?.ToString() ?? string.Empty)
			.Where(site => !string.IsNullOrWhiteSpace(site))
			.ToImmutableArray();
		var result = ArchitectureDependencySiteNames.All.Where(site => selected.Contains(site, StringComparer.Ordinal)).ToImmutableArray();

		return result;
	}
}
