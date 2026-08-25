using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Styling;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

internal static partial class ArchitectureLayerCodeLensPopout
{
	private static UIElement CreateDetails(ArchitectureLayerIndicator indicator, ArchitectureEditorOptions options)
	{
		var body = new StackPanel
		{
			Margin = new Thickness(12, 0, 12, 12)
		};
		if (!indicator.IsInLayer)
		{
			var description = string.IsNullOrWhiteSpace(indicator.Description)
				? "This type is not matched by the current AnaalIJzer settings."
				: indicator.Description!;
			AddSection(body, "Status", [description]);
		}
		else
		{
			if (indicator.LayerAncestry.Length > 0)
			{
				AddSection(body, "Ancestry", [string.Join(" > ", indicator.LayerAncestry)]);
			}

			if (options.ShowLinearCallChainInBadges && indicator.LinearCallChain.Length > 1)
			{
				AddSection(body, "Call Chain", [string.Join(" -> ", indicator.LinearCallChain)]);
			}

			AddSection(body, "Can Be Called By", FilterGlobalLayerRules(indicator.LayersThatCanCallThisLayer, options));
			AddSection(body, "Can Call", FilterGlobalLayerRules(indicator.LayersThisLayerCanCall, options));
			if (!string.IsNullOrWhiteSpace(indicator.Description))
			{
				AddSection(body, "Description", [indicator.Description!]);
			}
		}

		var result = new ScrollViewer
		{
			Content = body,
			MaxHeight = 390,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};

		return result;
	}

	private static void AddSection(Panel parent, string title, ImmutableArray<string> values)
	{
		var section = new StackPanel
		{
			Margin = new Thickness(0, 8, 0, 0)
		};
		section.Children.Add(new TextBlock
		{
			Text = title,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 0, 0, 4)
		});
		if (values.Length == 0)
		{
			var empty = new TextBlock
			{
				Text = "none configured",
				TextWrapping = TextWrapping.Wrap
			};
			ArchitectureVisualStudioTheme.ApplyHintForeground(empty);
			section.Children.Add(empty);
		}
		else
		{
			foreach (var value in values)
			{
				section.Children.Add(new TextBlock
				{
					Text = value,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(0, 0, 0, 2)
				});
			}
		}

		parent.Children.Add(section);
	}

	private static ImmutableArray<string> FilterGlobalLayerRules(ImmutableArray<string> layers, ArchitectureEditorOptions options)
	{
		if (options.ShowGlobalLayerRulesInBadges)
		{
			return layers;
		}

		var result = layers
			.Where(layer => !IsGlobalLayerRule(layer))
			.ToImmutableArray();

		return result;
	}

	private static bool IsGlobalLayerRule(string layer)
	{
		var result = layer == "*" || layer.StartsWith("* ", StringComparison.Ordinal);

		return result;
	}
}
