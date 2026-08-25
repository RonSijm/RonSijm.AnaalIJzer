using System.ComponentModel;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

public sealed partial class AnaalIJzerOptionsPage
{
	[Category("Layer indicators")]
	[DisplayName("Show layer badges")]
	[Description("Show the resolved AnaalIJzer layer path after type declaration identifiers.")]
	public bool EnableInlineLayerBadges { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Show layer metadata above declarations")]
	[Description("Show a clickable CodeLens-style AnaalIJzer layer summary above type declarations.")]
	public bool EnableLayerCodeLens { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Show layer badges when not in layer")]
	[Description("Show a neutral badge after type declarations that are not assigned to any configured AnaalIJzer layer.")]
	public bool ShowLayerBadgesWhenNotInLayer { get; set; }

	[Category("Layer indicators")]
	[DisplayName("Show global layer rules in badge hover")]
	[Description("Include wildcard dependency rules such as '* (any layer)' in layer badge hover details.")]
	public bool ShowGlobalLayerRulesInBadges { get; set; }

	[Category("Layer indicators")]
	[DisplayName("Show mini call graph in badge hover")]
	[Description("Show a compact dependency chain in layer badge hover details when the configured graph is a straight one-to-one chain.")]
	public bool ShowLinearCallChainInBadges { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Gutter glyphs")]
	[Description("Show a glyph beside type declarations that belong to an AnaalIJzer layer.")]
	public bool EnableLayerGlyphs { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Highlight code in layer")]
	[Description("Show a region-like block highlight around type declarations that belong to an AnaalIJzer layer.")]
	public bool EnableLayerBlockHighlight { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Tint layer declaration text")]
	[Description("Apply the older line background tint to type declarations that belong to an AnaalIJzer layer.")]
	public bool EnableLayerBackgroundTint { get; set; }
}
