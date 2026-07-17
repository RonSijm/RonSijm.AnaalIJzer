using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Indicators;

public sealed class ArchitectureLayerIndicator(
    TextSpan declarationSpan,
    TextSpan identifierSpan,
    string typeName,
    string layerPath,
    ImmutableArray<string> layerAncestry,
    string? description,
    int paletteSlot,
    bool isInLayer = true,
    ImmutableArray<string> layersThatCanCallThisLayer = default,
    ImmutableArray<string> layersThisLayerCanCall = default,
    ImmutableArray<string> linearCallChain = default)
{
    public TextSpan DeclarationSpan { get; } = declarationSpan;

    public TextSpan IdentifierSpan { get; } = identifierSpan;

    public string TypeName { get; } = typeName;

    public string LayerPath { get; } = layerPath;

    public ImmutableArray<string> LayerAncestry { get; } = layerAncestry;

    public string? Description { get; } = description;

    public int PaletteSlot { get; } = paletteSlot;

    public bool IsInLayer { get; } = isInLayer;

    public ImmutableArray<string> LayersThatCanCallThisLayer { get; } = layersThatCanCallThisLayer.IsDefault ? ImmutableArray<string>.Empty : layersThatCanCallThisLayer;

    public ImmutableArray<string> LayersThisLayerCanCall { get; } = layersThisLayerCanCall.IsDefault ? ImmutableArray<string>.Empty : layersThisLayerCanCall;

    public ImmutableArray<string> LinearCallChain { get; } = linearCallChain.IsDefault ? ImmutableArray<string>.Empty : linearCallChain;
}
