using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.RuntimeConfig.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Core.Violations;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;

public static partial class LayerDependencyAnalyzer
{
    private static void AnalyzeTypeReference(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ObservedDependencyCollector? observedDependencies, CallerDependencyContext caller, Location reportLocation, ITypeSymbol depType, string site)
    {
        var callerMatch = caller.LayerMatch;
        var seenLayerDependencyTypeNames = new HashSet<string>(StringComparer.Ordinal);
        var seenNamespaceHierarchyDependencyKeys = new HashSet<string>(StringComparer.Ordinal);
        var unrecognizedGenericArguments = new List<ITypeSymbol>();
        var matchedAnyLayer = false;
        var outerTypeIsIgnored = false;
        var index = 0;

        foreach (var current in EnumerateTypeAndGenericArguments(depType))
        {
            var isOuter = index++ == 0;
            var effectiveSite = isOuter ? site : DependencySites.GenericArgument;

            var depTypeName = current.Name;
            if (string.IsNullOrEmpty(depTypeName))
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(current, caller.Symbol) || IsIgnoredRecognitionType(current))
            {
                outerTypeIsIgnored |= isOuter;
                continue;
            }

            var depNamespace = current.ContainingNamespace?.ToString() ?? string.Empty;
            if (config.HasNamespaceHierarchyPolicies)
            {
                var namespaceHierarchyEvaluation = config.EvaluateNamespaceHierarchyPolicies(caller.NamespaceName, depNamespace, effectiveSite);
                if (namespaceHierarchyEvaluation is not null)
                {
                    var dependencyKey = current.ToDisplayString() + "|" + effectiveSite;
                    if (seenNamespaceHierarchyDependencyKeys.Add(dependencyKey))
                    {
                        ReportNamespaceHierarchyViolation(context, violations, caller, depTypeName, depNamespace, reportLocation, effectiveSite, namespaceHierarchyEvaluation.Value);
                    }

                    continue;
                }
            }

            if (callerMatch is not { } matchedCaller)
            {
                continue;
            }

            var callerLayer = matchedCaller.Layer;
            var depMatch = config.Engine.FindLayer(depTypeName, depNamespace, current);

            if (depMatch is null)
            {
                if (!isOuter)
                {
                    unrecognizedGenericArguments.Add(current);
                }

                continue;
            }

            matchedAnyLayer = true;

            if (!seenLayerDependencyTypeNames.Add(depTypeName))
            {
                continue;
            }

            var (depLayer, matchedSuffix) = (depMatch.Value.Layer, depMatch.Value.MatchedSuffix);
            if (!depLayer.IsForbidden)
            {
                observedDependencies?.Record(caller.TypeName, callerLayer.Name, depTypeName, depLayer.Name, effectiveSite, reportLocation);
            }

            var ruleProperties = BuildRuleProperties(depMatch.Value, depTypeName);
            var decision = DependencyRuleEvaluator.Evaluate(config, matchedCaller, depMatch.Value, current, effectiveSite);

            if (decision.IsForbiddenLayer)
            {
                var properties = AddViolationProperties(
                    ruleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
                    caller.TypeName,
                    callerLayer.Name,
                    depTypeName,
                    depLayer.Name,
                    decision.Reason,
                    depLayer.Comment);
                if (isOuter && matchedSuffix is not null && depLayer.FixSuffix is not null)
                {
                    properties = properties
                        .Add(ArchitecturalDiagnostics.PropertyMatchedSuffix, matchedSuffix)
                        .Add(ArchitecturalDiagnostics.PropertyFixSuffix, depLayer.FixSuffix);
                }

                context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
                    ArchitecturalDiagnostics.TypeNotAllowed,
                    reportLocation,
                    properties,
                    caller.TypeName, callerLayer.Name, depTypeName, decision.Reason));

                violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.TypeNotAllowed, caller.TypeName, callerLayer.Name, depTypeName, depLayer.Name, decision.Reason, depLayer.Comment));
                continue;
            }

            if (decision.TypePolicyViolation is { } policyViolation)
            {
                var policyRuleProperties = policyViolation.Rule is { } policyRule
                    ? BuildRuleProperties(policyRule, depTypeName)
                    : ImmutableDictionary<string, string?>.Empty.Add(ArchitecturalDiagnostics.PropertyDepTypeName, depTypeName);
                var properties = AddViolationProperties(
                    policyRuleProperties.Add(ArchitecturalDiagnostics.PropertySite, effectiveSite),
                    caller.TypeName,
                    callerLayer.Name,
                    depTypeName,
                    policyViolation.DependencyLayerName,
                    policyViolation.Reason,
                    policyViolation.Comment);

                if (isOuter && policyViolation.Rule is { } matchedRule && policyViolation.MatchedSuffix is not null && matchedRule.Layer.FixSuffix is not null)
                {
                    properties = properties
                        .Add(ArchitecturalDiagnostics.PropertyMatchedSuffix, policyViolation.MatchedSuffix)
                        .Add(ArchitecturalDiagnostics.PropertyFixSuffix, matchedRule.Layer.FixSuffix);
                }

                context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
                    ArchitecturalDiagnostics.TypeNotAllowed,
                    reportLocation,
                    properties,
                    caller.TypeName, callerLayer.Name, depTypeName, policyViolation.Reason));

                violations.Add(new ViolationRecord(ArchitecturalDiagnosticIds.TypeNotAllowed, caller.TypeName, callerLayer.Name, depTypeName, policyViolation.DependencyLayerName, policyViolation.Reason, policyViolation.Comment));
                continue;
            }

            if (decision.IsAllowed)
            {
                if (config.Engine.HasEntryPointPolicies)
                {
                    var entryPointEvaluation = config.Engine.EvaluateBoundaryEntryPoints(matchedCaller, depMatch.Value, depTypeName, depNamespace, current, effectiveSite);
                    if (!entryPointEvaluation.IsAllowed)
                    {
                        ReportBoundaryEntryPointViolation(context, violations, caller.TypeName, callerLayer.Name, depTypeName, depLayer.Name, reportLocation, effectiveSite, ruleProperties, entryPointEvaluation);
                    }
                }

                continue;
            }

            ReportIllegalDependency(context, violations, caller.TypeName, callerLayer.Name, depTypeName, depLayer.Name, reportLocation, effectiveSite, config, ruleProperties, decision.EdgeEvaluation!.Value);
        }

        if (callerMatch is not { } callerLayerMatch)
        {
            return;
        }

        var matchedCallerLayer = callerLayerMatch.Layer;
        if (!matchedAnyLayer && !outerTypeIsIgnored && config.RequiresRecognizedDependencyAt(callerLayerMatch, site))
        {
            ReportUnrecognizedDependency(context, violations, caller.TypeName, matchedCallerLayer.Name, depType.Name, reportLocation, site);
        }

        if (!config.RequiresRecognizedDependencyAt(callerLayerMatch, DependencySites.GenericArgument))
        {
            return;
        }

        foreach (var argument in unrecognizedGenericArguments)
        {
            if (seenLayerDependencyTypeNames.Add(argument.Name))
            {
                ReportUnrecognizedDependency(context, violations, caller.TypeName, matchedCallerLayer.Name, argument.Name, reportLocation, DependencySites.GenericArgument);
            }
        }
    }
}