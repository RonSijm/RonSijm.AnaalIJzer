using Microsoft.VisualStudio.Text.Editor;
using RonSijm.AnaalIJzer.Indicators;

namespace RonSijm.AnaalIJzer.VisualStudio.LayerIndicators;

internal sealed class ArchitectureLayerGlyphTag(ArchitectureLayerIndicator indicator) : IGlyphTag
{
    public ArchitectureLayerIndicator Indicator { get; } = indicator;
}
