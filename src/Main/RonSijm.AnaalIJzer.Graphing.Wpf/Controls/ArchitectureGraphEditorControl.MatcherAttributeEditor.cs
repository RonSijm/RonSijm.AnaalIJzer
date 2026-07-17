using System.Collections.Immutable;
using System.Windows.Controls;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private sealed class MatcherAttributeEditor(StackPanel panel, ComboBox attributeName, TextBox attributeValue)
    {
        public StackPanel Panel { get; } = panel;

        public void SetAttributeNames(ImmutableArray<string> attributeNames)
		{
			var current = attributeName.SelectedItem?.ToString();
			attributeName.Items.Clear();
			foreach (var name in attributeNames)
			{
				attributeName.Items.Add(name);
			}

			attributeName.SelectedItem = current is not null && attributeNames.Contains(current, StringComparer.Ordinal) ? current : attributeNames.FirstOrDefault();
		}

		public bool TryGetAttributes(out ImmutableDictionary<string, string> attributes, out string message)
		{
			var name = attributeName.SelectedItem?.ToString()?.Trim();
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

			attributes = ImmutableDictionary.CreateRange(StringComparer.Ordinal, new[] { new KeyValuePair<string, string>(name!, value) });
			message = string.Empty;
			return true;
		}
	}
}
