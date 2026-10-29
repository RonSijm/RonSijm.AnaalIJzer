using Microsoft.CodeAnalysis.Text;
using RonSijm.IJzer.Core.Findings;

namespace RonSijm.IJzer.Core.Indicators;

public sealed class ArchitectureVisibilityPolicyIndicator(
    TextSpan span,
    string declarationName,
    string declarationTarget,
    string declaredAccessibility,
    bool isEffectivelyExternallyVisible,
    string layerPath,
    string reason,
    string? description,
    string configurationPath,
    int configurationLine)
{
    public TextSpan Span { get; } = span;
    public string DeclarationName { get; } = declarationName;
    public string DeclarationTarget { get; } = declarationTarget;
    public string DeclaredAccessibility { get; } = declaredAccessibility;
    public bool IsEffectivelyExternallyVisible { get; } = isEffectivelyExternallyVisible;
    public string LayerPath { get; } = layerPath;
    public string Reason { get; } = reason;
    public string? Description { get; } = description;
    public string ConfigurationPath { get; } = configurationPath;
    public int ConfigurationLine { get; } = configurationLine;
    public string DiagnosticId => ArchitecturalDiagnosticIds.VisibilityNotAllowed;
}