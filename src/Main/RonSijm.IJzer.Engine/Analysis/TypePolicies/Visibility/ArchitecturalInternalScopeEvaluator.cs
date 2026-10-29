using Microsoft.CodeAnalysis;
using RonSijm.IJzer.Core.LayerModel;
using RonSijm.IJzer.Core.Visibility.Annotations;
using AnalyzerConfig = RonSijm.IJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.IJzer.Engine.Analysis.TypePolicies.Visibility;

internal static class ArchitecturalInternalScopeEvaluator
{
    internal static bool IsReferenceAllowed(ISymbol callerSymbol, ArchitectureInternalRestriction restriction, AnalyzerConfig config, out string callerLayer, out string targetLayer, out string reason)
    {
        var callerType = callerSymbol.GetArchitectureOwnerType();
        callerLayer = FindLayerPath(config, callerType);
        targetLayer = FindLayerPath(config, restriction.OwnerType);

        switch (restriction.Scope)
        {
            case ArchitectureInternalScope.Class when callerType.IsInArchitectureTypeFamily(restriction.OwnerType):
                reason = string.Empty;
                return true;
            case ArchitectureInternalScope.Class:
                reason = $"[{restriction.AttributeDisplayName.Replace("Attribute", string.Empty)}] permits references only from '{restriction.OwnerType?.ToDisplayString() ?? "the declaring type"}' and its nested type family";
                return false;
            case ArchitectureInternalScope.Assembly when SymbolEqualityComparer.Default.Equals(callerSymbol.ContainingAssembly, restriction.OwnerAssembly):
                reason = string.Empty;
                return true;
            case ArchitectureInternalScope.Assembly:
                reason = $"[{restriction.AttributeDisplayName.Replace("Attribute", string.Empty)}] permits references only from assembly '{restriction.OwnerAssembly.Name}'";
                return false;
            case ArchitectureInternalScope.Layer when string.IsNullOrEmpty(targetLayer):
                reason = $"[{restriction.AttributeDisplayName.Replace("Attribute", string.Empty)}] requires '{restriction.OwnerType?.ToDisplayString() ?? restriction.AnnotatedSymbol.Name}' to belong to a configured layer";
                return false;
            case ArchitectureInternalScope.Layer when string.IsNullOrEmpty(callerLayer):
                reason = $"[{restriction.AttributeDisplayName.Replace("Attribute", string.Empty)}] permits references only from layer '{targetLayer}' or its descendants, but the caller has no configured layer";
                return false;
            case ArchitectureInternalScope.Layer when PathMatches(callerLayer, targetLayer):
                reason = string.Empty;
                return true;
            case ArchitectureInternalScope.Layer:
                reason = $"[{restriction.AttributeDisplayName.Replace("Attribute", string.Empty)}] permits references only from layer '{targetLayer}' or its descendants; caller layer is '{callerLayer}'";
                return false;
            default:
                reason = "the architectural-internal scope could not be evaluated";
                return false;
        }
    }

    internal static bool IsExposureProtected(ISymbol exposingSymbol, ArchitectureInternalRestriction targetRestriction, AnalyzerConfig config)
    {
        foreach (var exposingRestriction in exposingSymbol.GetArchitectureInternalRestrictions())
        {
            if (Protects(exposingRestriction, targetRestriction, config))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Protects(ArchitectureInternalRestriction exposing, ArchitectureInternalRestriction target, AnalyzerConfig config)
    {
        var sameAssembly = SymbolEqualityComparer.Default.Equals(exposing.OwnerAssembly, target.OwnerAssembly);
        var result = target.Scope switch
        {
            ArchitectureInternalScope.Class => exposing.Scope == ArchitectureInternalScope.Class
                                               && exposing.OwnerType.IsInArchitectureTypeFamily(target.OwnerType),
            ArchitectureInternalScope.Assembly => sameAssembly
                                                  && exposing.Scope is ArchitectureInternalScope.Class or ArchitectureInternalScope.Assembly,
            ArchitectureInternalScope.Layer => ProtectsLayer(exposing, target, config),
            _ => false
        };

        return result;
    }

    private static bool ProtectsLayer(ArchitectureInternalRestriction exposing, ArchitectureInternalRestriction target, AnalyzerConfig config)
    {
        if (exposing.Scope == ArchitectureInternalScope.Class)
        {
            var exposingLayer = FindLayerPath(config, exposing.OwnerType);
            var targetLayer = FindLayerPath(config, target.OwnerType);
            var classResult = !string.IsNullOrEmpty(exposingLayer)
                              && !string.IsNullOrEmpty(targetLayer)
                              && PathMatches(exposingLayer, targetLayer);

            return classResult;
        }

        if (exposing.Scope != ArchitectureInternalScope.Layer)
        {
            return false;
        }

        var exposingPath = FindLayerPath(config, exposing.OwnerType);
        var targetPath = FindLayerPath(config, target.OwnerType);
        var result = !string.IsNullOrEmpty(exposingPath)
                     && !string.IsNullOrEmpty(targetPath)
                     && PathMatches(exposingPath, targetPath);

        return result;
    }

    private static string FindLayerPath(AnalyzerConfig config, INamedTypeSymbol? type)
    {
        if (type is null || !config.Engine.HasLayers)
        {
            return string.Empty;
        }

        var match = config.Engine.FindLayer(type.Name, type.ContainingNamespace?.ToDisplayString() ?? string.Empty, type);
        var result = match?.Layer.Name ?? string.Empty;

        return result;
    }

    private static bool PathMatches(string candidate, string boundary)
    {
        var result = string.Equals(candidate, boundary, StringComparison.Ordinal)
                     || candidate.StartsWith(boundary + "/", StringComparison.Ordinal);

        return result;
    }
}
