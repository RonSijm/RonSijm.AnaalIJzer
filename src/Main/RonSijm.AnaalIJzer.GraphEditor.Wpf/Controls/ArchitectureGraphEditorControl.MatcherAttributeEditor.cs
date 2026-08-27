using System.Collections.Immutable;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private sealed class MatcherAttributeEditor(StackPanel panel, ComboBox attributeName, TextBox attributeValue)
	{
		public StackPanel Panel { get; } = panel;

		public void SetAttributeNames(ImmutableArray<string> attributeNames)
		{
			var current = attributeName.SelectedItem as string;
			attributeName.Items.Clear();
			foreach (var name in attributeNames)
			{
				attributeName.Items.Add(name);
			}

			attributeName.SelectedItem = current is not null && attributeNames.Contains(current, StringComparer.Ordinal) ? current : attributeNames.FirstOrDefault();
		}

		public bool TryGetAttributes(out ImmutableDictionary<string, string> attributes, out string message)
		{
			var name = (attributeName.SelectedItem as string)?.Trim();
			var value = attributeValue.Text.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				attributes = ImmutableDictionary<string, string>.Empty;
				message = "Choose a matcher attribute.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(value))
			{
				attributes = ImmutableDictionary<string, string>.Empty;
				message = "Enter a value for " + name + ".";
				return false;
			}

			var validatedName = name;
			attributes = ImmutableDictionary.CreateRange(StringComparer.Ordinal, [new KeyValuePair<string, string>(validatedName, value)]);
			message = string.Empty;
			return true;
		}
	}
}
