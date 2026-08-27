using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed class ArchitectureLayerCreationDialog : Window
{
	private readonly ArchitectureGraphCanvasTheme _theme;
	private readonly TextBox _name;
	private readonly ComboBox _matcherKind;
	private readonly ComboBox _attributeName;
	private readonly TextBox _attributeValue;
	private readonly TextBlock _error;

	private ArchitectureLayerCreationDialog(ArchitectureGraphCanvasTheme theme)
	{
		this._theme = theme;
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
		_name = CreateTextBox();
		root.Children.Add(_name);
		root.Children.Add(CreateLabel("Matcher kind"));
		_matcherKind = CreateComboBox("Class", "Namespace", "Assembly");
		_matcherKind.SelectionChanged += (_, _) => UpdateAttributeNames();
		root.Children.Add(_matcherKind);
		root.Children.Add(CreateLabel("Matcher attribute"));
		var row = new Grid();
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_attributeName = CreateComboBox();
		Grid.SetColumn(_attributeName, 0);
		row.Children.Add(_attributeName);
		_attributeValue = CreateTextBox();
		Grid.SetColumn(_attributeValue, 2);
		row.Children.Add(_attributeValue);
		root.Children.Add(row);
		_error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.IndianRed, Margin = new Thickness(0, 8, 0, 0) };
		root.Children.Add(_error);
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
		Loaded += (_, _) => _name.Focus();
	}

	private ArchitectureLayerCreationRequest? Request { get; set; }

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
		if (string.IsNullOrWhiteSpace(_name.Text))
		{
			_error.Text = "Layer name is required.";
			return;
		}

		if (_name.Text.Contains("/"))
		{
			_error.Text = "Layer names may not contain '/'.";
			return;
		}

		var key = _attributeName.SelectedItem as string ?? string.Empty;
		if (string.IsNullOrWhiteSpace(key))
		{
			_error.Text = "Choose a matcher attribute.";
			return;
		}

		if (string.IsNullOrWhiteSpace(_attributeValue.Text))
		{
			_error.Text = "Matcher value is required.";
			return;
		}

		var attributes = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		attributes[key] = _attributeValue.Text.Trim();
		Request = new ArchitectureLayerCreationRequest(_name.Text.Trim(), _matcherKind.SelectedItem as string ?? "Class", attributes.ToImmutable());
		DialogResult = true;
	}

	private void UpdateAttributeNames()
	{
		_attributeName.Items.Clear();
		foreach (var option in MatcherAttributeOptions.GetNames(_matcherKind.SelectedItem as string))
		{
			_attributeName.Items.Add(option);
		}

		_attributeName.SelectedIndex = _attributeName.Items.Count > 0 ? 0 : -1;
	}

	private TextBlock CreateLabel(string text)
	{
		var result = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2), Foreground = _theme.Foreground };

		return result;
	}

	private TextBox CreateTextBox()
	{
		var result = new TextBox { Background = _theme.SurfaceBackground, Foreground = _theme.Foreground, BorderBrush = _theme.Border };

		return result;
	}

	private ComboBox CreateComboBox(params string[] items)
	{
		var result = new ComboBox { Background = _theme.SurfaceBackground, Foreground = _theme.Foreground, BorderBrush = _theme.Border };
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
		var result = new Button { Content = text, MinWidth = 82, Margin = new Thickness(6, 0, 0, 0), Foreground = _theme.Foreground, BorderBrush = _theme.Border };

		return result;
	}
}
