using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

internal sealed class ArchitectureLayerCreationDialog : Window
{
	private readonly ArchitectureGraphCanvasTheme theme;
	private readonly TextBox name;
	private readonly ComboBox matcherKind;
	private readonly ComboBox attributeName;
	private readonly TextBox attributeValue;
	private readonly TextBlock error;

	private ArchitectureLayerCreationDialog(ArchitectureGraphCanvasTheme theme)
	{
		this.theme = theme;
		Title = "Add AnaalIJzer layer";
		Width = 420;
		SizeToContent = SizeToContent.Height;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		ResizeMode = ResizeMode.NoResize;
		Background = theme.SurfaceBackground;
		Foreground = theme.Foreground;
		theme.ApplyToRoot(this);
		var root = new StackPanel { Margin = new Thickness(14) };
		theme.ApplyToRoot(root);
		root.Children.Add(CreateLabel("Layer name"));
		name = CreateTextBox();
		root.Children.Add(name);
		root.Children.Add(CreateLabel("Matcher kind"));
		matcherKind = CreateComboBox("Class", "Namespace", "Assembly");
		matcherKind.SelectionChanged += (_, _) => UpdateAttributeNames();
		root.Children.Add(matcherKind);
		root.Children.Add(CreateLabel("Matcher attribute"));
		var row = new Grid();
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		attributeName = CreateComboBox();
		Grid.SetColumn(attributeName, 0);
		row.Children.Add(attributeName);
		attributeValue = CreateTextBox();
		Grid.SetColumn(attributeValue, 2);
		row.Children.Add(attributeValue);
		root.Children.Add(row);
		error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.IndianRed, Margin = new Thickness(0, 8, 0, 0) };
		root.Children.Add(error);
		var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
		var cancel = CreateButton("Cancel");
		cancel.Click += (_, _) => DialogResult = false;
		var ok = CreateButton("Add layer");
		ok.Click += (_, _) => Accept();
		buttons.Children.Add(cancel);
		buttons.Children.Add(ok);
		root.Children.Add(buttons);
		UpdateAttributeNames();
		Content = root;
		Loaded += (_, _) => name.Focus();
	}

	public ArchitectureLayerCreationRequest? Request { get; private set; }

	public static ArchitectureLayerCreationRequest? Prompt(Window? owner, ArchitectureGraphCanvasTheme theme)
	{
		var dialog = new ArchitectureLayerCreationDialog(theme);
		if (owner is not null)
		{
			dialog.Owner = owner;
		}

		var accepted = dialog.ShowDialog() == true;
		var result = accepted ? dialog.Request : null;

		return result;
	}

	private void Accept()
	{
		if (string.IsNullOrWhiteSpace(name.Text))
		{
			error.Text = "Layer name is required.";
			return;
		}

		if (name.Text.Contains("/"))
		{
			error.Text = "Layer names may not contain '/'.";
			return;
		}

		var key = attributeName.SelectedItem?.ToString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(key))
		{
			error.Text = "Choose a matcher attribute.";
			return;
		}

		if (string.IsNullOrWhiteSpace(attributeValue.Text))
		{
			error.Text = "Matcher value is required.";
			return;
		}

		var attributes = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		attributes[key] = attributeValue.Text.Trim();
		Request = new ArchitectureLayerCreationRequest(name.Text.Trim(), matcherKind.SelectedItem?.ToString() ?? "Class", attributes.ToImmutable());
		DialogResult = true;
	}

	private void UpdateAttributeNames()
	{
		attributeName.Items.Clear();
		foreach (var option in MatcherAttributeOptions.GetNames(matcherKind.SelectedItem?.ToString()))
		{
			attributeName.Items.Add(option);
		}

		attributeName.SelectedIndex = attributeName.Items.Count > 0 ? 0 : -1;
	}

	private TextBlock CreateLabel(string text)
	{
		var result = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2), Foreground = theme.Foreground };

		return result;
	}

	private TextBox CreateTextBox()
	{
		var result = new TextBox { Background = theme.SurfaceBackground, Foreground = theme.Foreground, BorderBrush = theme.Border };

		return result;
	}

	private ComboBox CreateComboBox(params string[] items)
	{
		var result = new ComboBox { Background = theme.SurfaceBackground, Foreground = theme.Foreground, BorderBrush = theme.Border };
		foreach (var item in items)
		{
			result.Items.Add(item);
		}

		if (result.Items.Count > 0)
		{
			result.SelectedIndex = 0;
		}

		return result;
	}

	private Button CreateButton(string text)
	{
		var result = new Button { Content = text, MinWidth = 82, Margin = new Thickness(6, 0, 0, 0), Foreground = theme.Foreground, BorderBrush = theme.Border };

		return result;
	}
}
