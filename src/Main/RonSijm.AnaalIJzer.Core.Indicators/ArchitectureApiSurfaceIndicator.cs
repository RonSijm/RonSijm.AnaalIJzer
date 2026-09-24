using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Core.Indicators;

public sealed class ArchitectureApiSurfaceIndicator(
    TextSpan span,
    string apiMemberName,
    string callerTypeName,
    string callerLayerPath,
    string exposedTypeName,
    string exposedLayerPath,
    string site,
    string reason,
    string? description,
    string configurationPath,
    int configurationLine,
    string diagnosticId = ArchitecturalDiagnosticIds.ApiExposureNotAllowed,
    string? exposurePath = null,
    int? exposureDepth = null,
    ImmutableArray<ArchitectureExposurePathSegment> exposureSegments = default)
{
    public TextSpan Span { get; } = span;
    public string ApiMemberName { get; } = apiMemberName;
    public string CallerTypeName { get; } = callerTypeName;
    public string CallerLayerPath { get; } = callerLayerPath;
    public string ExposedTypeName { get; } = exposedTypeName;
    public string ExposedLayerPath { get; } = exposedLayerPath;
    public string Site { get; } = site;
    public string Reason { get; } = reason;
    public string? Description { get; } = description;
    public string ConfigurationPath { get; } = configurationPath;
    public int ConfigurationLine { get; } = configurationLine;
    public string DiagnosticId { get; } = diagnosticId;
    public string? ExposurePath { get; } = exposurePath;
    public int? ExposureDepth { get; } = exposureDepth;
    public ImmutableArray<ArchitectureExposurePathSegment> ExposureSegments { get; } = exposureSegments.IsDefault ? ImmutableArray<ArchitectureExposurePathSegment>.Empty : exposureSegments;
    public bool IsTransitive => !string.IsNullOrWhiteSpace(ExposurePath) || ExposureDepth.HasValue;
}