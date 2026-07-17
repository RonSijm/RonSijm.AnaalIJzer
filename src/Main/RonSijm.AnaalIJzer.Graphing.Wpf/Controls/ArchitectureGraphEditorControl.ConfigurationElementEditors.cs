using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private void AddConfigurationElementEditors(
		StackPanel panel,
		string title,
		ImmutableArray<ArchitectureConfigurationElementDetails> elements,
		ArchitectureLayerEditHandle layerHandle,
		string containerKind,
		ImmutableArray<string> elementKinds)
	{
		panel.Children.Add(CreateSectionTitle(title));
		if (elements.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var element in elements)
		{
			panel.Children.Add(CreateConfigurationElementEditor(element));
		}

		var addPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
		addPanel.Children.Add(new TextBlock { Text = "Add", FontWeight = FontWeights.SemiBold });
		var kind = new ComboBox { Margin = new Thickness(0, 2, 0, 0), IsEnabled = layerHandle.CanEdit };
		foreach (var elementKind in elementKinds)
		{
			kind.Items.Add(elementKind);
		}

		kind.SelectedIndex = 0;
		addPanel.Children.Add(kind);
		var attributeEditor = CreateMatcherAttributeEditor(kind, layerHandle.CanEdit);
		addPanel.Children.Add(attributeEditor.Panel);
		var add = new Button { Content = "Add " + title.ToLowerInvariant(), Margin = new Thickness(0, 4, 0, 0), IsEnabled = layerHandle.CanEdit };
		add.Click += (_, _) =>
		{
			if (!attributeEditor.TryGetAttributes(out var parsedAttributes, out var message))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure(message));
				return;
			}

			var elementKind = kind.SelectedItem?.ToString() ?? "Class";
			var result = containerKind == "LayerMatcher"
				? ArchitectureConfigurationEditService.AddLayerMatcher(layerHandle, elementKind, parsedAttributes)
				: ArchitectureConfigurationEditService.AddTypePolicyMatcher(layerHandle, containerKind, elementKind, parsedAttributes);
			HandleEditResult(result);
		};
		addPanel.Children.Add(add);
		panel.Children.Add(addPanel);
	}

	private void AddLayerStructureEditor(StackPanel panel, ArchitectureLayerEditHandle handle)
	{
		panel.Children.Add(CreateSectionTitle("Structure"));
		var source = new ArchitectureConfigurationSource(handle.SourceKind, handle.SourcePath);
		AddLayerCreationEditor(panel, source, handle.LayerPath, "Child layers");
		panel.Children.Add(new TextBlock { Text = "Move to parent path", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		var moveTarget = new TextBox
		{
			Text = handle.ParentPath,
			TextWrapping = TextWrapping.Wrap,
			IsEnabled = handle.CanEdit
		};
		panel.Children.Add(moveTarget);
		var move = new Button { Content = "Move layer", Margin = new Thickness(0, 4, 0, 0), IsEnabled = handle.CanEdit };
		move.Click += (_, _) =>
		{
			if (confirmationHandler("Move layer '" + handle.LayerPath + "' to parent path '" + (string.IsNullOrWhiteSpace(moveTarget.Text) ? "root" : moveTarget.Text.Trim()) + "'?"))
			{
				HandleEditResult(ArchitectureConfigurationEditService.MoveLayer(handle, moveTarget.Text), true);
			}
		};
		panel.Children.Add(move);
		var remove = CreateDangerButton("Remove layer", handle.CanEdit);
		remove.Margin = new Thickness(0, 8, 0, 0);
		remove.Click += (_, _) =>
		{
			if (confirmationHandler("Remove layer '" + handle.LayerPath + "' and its nested settings?"))
			{
				HandleEditResult(ArchitectureConfigurationEditService.RemoveLayer(handle), true);
			}
		};
		panel.Children.Add(remove);
	}

	private void AddLayerCreationEditor(StackPanel panel, ArchitectureConfigurationSource source, string parentPath, string title)
	{
		panel.Children.Add(CreateSectionTitle(title));
		panel.Children.Add(new TextBlock { Text = "New layer name", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2) });
		var name = new TextBox { Text = string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(name);
		var matcherKind = new ComboBox { Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		matcherKind.Items.Add("Class");
		matcherKind.Items.Add("Namespace");
		matcherKind.Items.Add("Assembly");
		matcherKind.SelectedIndex = 0;
		panel.Children.Add(matcherKind);
		var matcherAttributes = CreateMatcherAttributeEditor(matcherKind, source.CanEdit);
		panel.Children.Add(matcherAttributes.Panel);
		var add = new Button { Content = "Add layer", Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) =>
		{
			if (!matcherAttributes.TryGetAttributes(out var parsedAttributes, out var message))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure(message));
				return;
			}

			HandleEditResult(ArchitectureConfigurationEditService.AddLayer(source, parentPath, name.Text, matcherKind.SelectedItem?.ToString() ?? "Class", parsedAttributes));
		};
		panel.Children.Add(add);
	}

	private void AddGlobalConfigurationElementEditors(
		StackPanel panel,
		string title,
		ImmutableArray<ArchitectureConfigurationElementDetails> elements,
		ArchitectureConfigurationSource source,
		string policyKind)
	{
		panel.Children.Add(CreateSectionTitle(title));
		if (elements.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var element in elements)
		{
			panel.Children.Add(CreateConfigurationElementEditor(element));
		}

		var addPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
		addPanel.Children.Add(new TextBlock { Text = "Add", FontWeight = FontWeights.SemiBold });
		var kind = new ComboBox { Margin = new Thickness(0, 2, 0, 0), IsEnabled = source.CanEdit };
		kind.Items.Add("Class");
		kind.Items.Add("Namespace");
		kind.SelectedIndex = 0;
		addPanel.Children.Add(kind);
		var attributeEditor = CreateMatcherAttributeEditor(kind, source.CanEdit);
		addPanel.Children.Add(attributeEditor.Panel);
		var add = new Button { Content = "Add " + title.ToLowerInvariant(), Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) =>
		{
			if (!attributeEditor.TryGetAttributes(out var parsedAttributes, out var message))
			{
				HandleEditResult(ArchitectureConfigurationEditResult.Failure(message));
				return;
			}

			HandleEditResult(ArchitectureConfigurationEditService.AddGlobalTypePolicyMatcher(source, policyKind, kind.SelectedItem?.ToString() ?? "Class", parsedAttributes));
		};
		addPanel.Children.Add(add);
		panel.Children.Add(addPanel);
	}

	private void AddIncludeEditors(StackPanel panel, ArchitectureConfigurationSource source, ImmutableArray<ArchitectureConfigurationElementDetails> includes)
	{
		panel.Children.Add(CreateSectionTitle("Includes"));
		if (includes.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("None configured.", new Thickness(0, 0, 0, 4)));
		}

		foreach (var include in includes)
		{
			panel.Children.Add(CreateConfigurationElementEditor(include));
		}

		var path = new TextBox { Text = string.Empty, TextWrapping = TextWrapping.Wrap, IsEnabled = source.CanEdit };
		panel.Children.Add(path);
		var add = new Button { Content = "Add Include", Margin = new Thickness(0, 4, 0, 0), IsEnabled = source.CanEdit };
		add.Click += (_, _) => HandleEditResult(ArchitectureConfigurationEditService.AddInclude(source, path.Text));
		panel.Children.Add(add);
	}

	private UIElement CreateConfigurationElementEditor(ArchitectureConfigurationElementDetails element)
	{
		var expander = new Expander
		{
			Header = element.Summary,
			IsExpanded = false,
			Margin = new Thickness(0, 4, 0, 0)
		};
		var panel = new StackPanel();
		AddReadOnlyRow(panel, "Element", element.ContainerKind + " / " + element.ElementKind);
		var attributes = new TextBox
		{
			Text = FormatAttributes(element.Attributes),
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			MinHeight = 72,
			IsEnabled = element.Handle.CanEdit
		};
		panel.Children.Add(CreateHintTextBlock("Use one key=value attribute per line.", new Thickness(0, 2, 0, 2)));
		panel.Children.Add(attributes);
		AutoSaveOnLostFocus(attributes, () =>
		{
			if (!TryParseAttributes(attributes.Text, out var parsedAttributes, out var message))
			{
				return ArchitectureConfigurationEditResult.Failure(message);
			}

			return ArchitectureConfigurationEditService.SetConfigurationElementAttributes(element.Handle, parsedAttributes);
		}, element.Handle.CanEdit, true);
		panel.Children.Add(new TextBlock { Text = "Child XML (Exceptions/Fix)", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2) });
		panel.Children.Add(CreateHintTextBlock("Use this for scoped child elements such as Exceptions or Fix.", new Thickness(0, 0, 0, 2)));
		var childXml = new TextBox
		{
			Text = element.ChildXml,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			MinHeight = 72,
			IsEnabled = element.Handle.CanEdit
		};
		panel.Children.Add(childXml);
		AutoSaveOnLostFocus(childXml, () => ArchitectureConfigurationEditService.SetConfigurationElementChildren(element.Handle, childXml.Text), element.Handle.CanEdit, true);
		var remove = CreateDangerButton("Remove", element.Handle.CanEdit);
		remove.Margin = new Thickness(0, 4, 0, 0);
		remove.Click += (_, _) =>
		{
			if (confirmationHandler("Remove '" + element.Summary + "'?"))
			{
				HandleEditResult(ArchitectureConfigurationEditService.RemoveConfigurationElement(element.Handle), true);
			}
		};
		panel.Children.Add(remove);
		expander.Content = panel;

		return expander;
	}

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
			editor.SetAttributeNames(GetMatcherAttributeNames(elementKind.SelectedItem?.ToString()));
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

	private static string FormatAttributes(ImmutableDictionary<string, string> attributes)
	{
		var result = string.Join(Environment.NewLine, attributes.OrderBy(attribute => attribute.Key, StringComparer.Ordinal).Select(attribute => attribute.Key + "=" + attribute.Value));

		return result;
	}

	private static bool TryParseAttributes(string text, out ImmutableDictionary<string, string> attributes, out string message)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
		{
			var line = rawLine.Trim();
			if (line.Length == 0)
			{
				continue;
			}

			var separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0)
			{
				attributes = ImmutableDictionary<string, string>.Empty;
				message = "Attributes must use key=value lines.";
				return false;
			}

			var key = line.Substring(0, separatorIndex).Trim();
			var value = line.Substring(separatorIndex + 1).Trim().Trim('"');
			if (key.Length == 0)
			{
				attributes = ImmutableDictionary<string, string>.Empty;
				message = "Attribute names may not be empty.";
				return false;
			}

			builder[key] = value;
		}

		attributes = builder.ToImmutable();
		message = string.Empty;
		return true;
	}
}
