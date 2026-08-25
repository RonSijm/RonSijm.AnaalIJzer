using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Indicators;

public sealed class ArchitectureDependencySiteIndicator(
    TextSpan span,
    string site,
    string callerTypeName,
    string callerLayerPath,
    string dependencyTypeName,
    string? dependencyLayerPath,
    int dependencyLayerPaletteSlot,
    ArchitectureDependencySiteStatus status,
    string? diagnosticId,
    string tooltip,
    string? reason = null)
{
    public TextSpan Span { get; } = span;

    public string Site { get; } = site;

    public string CallerTypeName { get; } = callerTypeName;

    public string CallerLayerPath { get; } = callerLayerPath;

    public string DependencyTypeName { get; } = dependencyTypeName;

    public string? DependencyLayerPath { get; } = dependencyLayerPath;

    public int DependencyLayerPaletteSlot { get; } = dependencyLayerPaletteSlot;

    public ArchitectureDependencySiteStatus Status { get; } = status;

    public string? DiagnosticId { get; } = diagnosticId;

    public string Tooltip { get; } = tooltip;

    public string Reason { get; } = reason ?? tooltip;
}
