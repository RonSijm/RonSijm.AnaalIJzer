using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
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

			return editService.SetConfigurationElementAttributes(element.Handle, parsedAttributes);
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
		AutoSaveOnLostFocus(childXml, () => editService.SetConfigurationElementChildren(element.Handle, childXml.Text), element.Handle.CanEdit, true);
		var remove = CreateDangerButton("Remove", element.Handle.CanEdit);
		remove.Margin = new Thickness(0, 4, 0, 0);
		remove.Click += (_, _) =>
		{
			if (confirmationHandler("Remove '" + element.Summary + "'?"))
			{
				HandleEditResult(editService.RemoveConfigurationElement(element.Handle), true);
			}
		};
		panel.Children.Add(remove);
		expander.Content = panel;

		return expander;
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
