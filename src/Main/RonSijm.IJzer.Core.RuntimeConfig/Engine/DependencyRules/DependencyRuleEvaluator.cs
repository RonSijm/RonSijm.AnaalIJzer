using Microsoft.CodeAnalysis;
using RonSijm.IJzer.Core.DependencyRules;
using RonSijm.IJzer.Core.Findings;
using RonSijm.IJzer.Core.Indicators;
using RonSijm.IJzer.Core.LayerModel;
using AnalyzerConfig = RonSijm.IJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.IJzer.Core.RuntimeConfig.Engine.DependencyRules;

public static class DependencyRuleEvaluator
{
    public static DependencyRuleDecision Evaluate(AnalyzerConfig config, LayerMatch callerMatch, LayerMatch dependencyMatch, ITypeSymbol dependencyType, string site)
    {
        var dependencyLayer = dependencyMatch.Layer;
        if (dependencyLayer.IsForbidden)
        {
            var forbiddenReason = dependencyLayer.Comment is null
                ? "the type matches a global <Forbidden> rule"
                : "the type matches a global <Forbidden> rule: " + dependencyLayer.Comment;
            var forbiddenResult = new DependencyRuleDecision(
                dependencyLayer.Name,
                ArchitectureDependencySiteStatus.TypePolicyViolation,
                ArchitecturalDiagnosticIds.TypeNotAllowed,
                forbiddenReason,
                null,
                null,
                true);

            return forbiddenResult;
        }

        if (config.Engine.EvaluateTypePolicy(dependencyMatch, dependencyType.Name, dependencyType.ContainingNamespace?.ToDisplayString() ?? string.Empty, dependencyType) is { } typePolicyViolation)
        {
            var policyResult = new DependencyRuleDecision(
                typePolicyViolation.DependencyLayerName,
                ArchitectureDependencySiteStatus.TypePolicyViolation,
                ArchitecturalDiagnosticIds.TypeNotAllowed,
                typePolicyViolation.Reason,
                typePolicyViolation,
                null,
                false);

            return policyResult;
        }

        var edgeEvaluation = config.Graph.EvaluateDependency(callerMatch, dependencyMatch, site);
        if (edgeEvaluation.IsAllowed)
        {
            var allowedResult = new DependencyRuleDecision(
                dependencyLayer.Name,
                ArchitectureDependencySiteStatus.Allowed,
                null,
                "allowed by configured dependency rules",
                null,
                edgeEvaluation,
                false);

            return allowedResult;
        }

        var status = GetDeniedStatus(callerMatch.Layer.Name, dependencyLayer.Name, edgeEvaluation, config);
        var diagnosticId = status switch
        {
            ArchitectureDependencySiteStatus.WrongDirection => ArchitecturalDiagnosticIds.DependencyReverseDirection,
            ArchitectureDependencySiteStatus.SameLayer => ArchitecturalDiagnosticIds.DependencyPeerScope,
            _ => ArchitecturalDiagnosticIds.DependencyNotAllowed
        };
        var reason = status == ArchitectureDependencySiteStatus.SameLayer && !edgeEvaluation.IsDeniedBySiteFilter
            ? $"types in the same layer ('{callerMatch.Layer.Name}') may not depend on each other"
            : status == ArchitectureDependencySiteStatus.WrongDirection && !edgeEvaluation.IsDeniedBySiteFilter
                ? $"this dependency goes the wrong direction - the reverse ('{dependencyLayer.Name}' -> '{callerMatch.Layer.Name}') is configured"
                : edgeEvaluation.DenialReason;
        var deniedResult = new DependencyRuleDecision(
            dependencyLayer.Name,
            status,
            diagnosticId,
            reason,
            null,
            edgeEvaluation,
            false);

        return deniedResult;
    }

    private static ArchitectureDependencySiteStatus GetDeniedStatus(string callerLayerName, string dependencyLayerName, DependencyEdgeEvaluation edgeEvaluation, AnalyzerConfig config)
    {
        if (callerLayerName == dependencyLayerName)
        {
            return ArchitectureDependencySiteStatus.SameLayer;
        }

        if (edgeEvaluation.IsDeniedByBlockedEdge)
        {
            return ArchitectureDependencySiteStatus.Blocked;
        }

        if (config.Graph.HasEdge(edgeEvaluation.ScopePath, dependencyLayerName, callerLayerName))
        {
            return ArchitectureDependencySiteStatus.WrongDirection;
        }

        var result = edgeEvaluation.IsDeniedBySiteFilter
            ? ArchitectureDependencySiteStatus.SiteFiltered
            : ArchitectureDependencySiteStatus.MissingAllowedDependency;

        return result;
    }
}