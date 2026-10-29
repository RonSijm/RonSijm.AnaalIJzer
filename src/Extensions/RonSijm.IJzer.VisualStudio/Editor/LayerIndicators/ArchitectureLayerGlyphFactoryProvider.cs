using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using RonSijm.IJzer.VisualStudio.Editor.Tags;

namespace RonSijm.IJzer.VisualStudio.Editor.LayerIndicators;

[Export(typeof(IGlyphFactoryProvider))]
[Name("IJzerLayerGlyph")]
[Order(After = "VsTextMarker")]
[ContentType("CSharp")]
[TagType(typeof(ArchitectureLayerGlyphTag))]
internal sealed class ArchitectureLayerGlyphFactoryProvider : IGlyphFactoryProvider
{
    public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
    {
        var result = new ArchitectureLayerGlyphFactory();

        return result;
    }
}

internal sealed class ArchitectureLayerGlyphFactory : IGlyphFactory
{
    public UIElement? GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
    {
        var result = tag is ArchitectureLayerGlyphTag layerTag
            ? ArchitectureAdornmentFactory.CreateLayerGlyph(layerTag.Indicator)
            : null;

        return result;
    }
}