using Microsoft.VisualStudio.Text.Editor;
using RonSijm.IJzer.Core.Indicators;

namespace RonSijm.IJzer.VisualStudio.Editor.LayerIndicators;

internal sealed class ArchitectureLayerGlyphTag(ArchitectureLayerIndicator indicator) : IGlyphTag
{
    public ArchitectureLayerIndicator Indicator { get; } = indicator;
}