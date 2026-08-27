using Microsoft.VisualStudio.Text.Editor;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.VisualStudio.Editor.LayerIndicators;

internal sealed class ArchitectureLayerGlyphTag(ArchitectureLayerIndicator indicator) : IGlyphTag
{
    public ArchitectureLayerIndicator Indicator { get; } = indicator;
}
