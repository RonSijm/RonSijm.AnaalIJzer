using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.NameRules;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Analysis;
using RonSijm.AnaalIJzer.Core.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.NamingRules;

public static partial class LayerDependencyAnalyzer
{
    public static void AnalyzeIntraProceduralNameRules(OperationBlockAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
    {
        if (!TryGetOperationBlockCaller(context, config, out var callerTypeName, out var callerLayerName, out var callerMatch))
        {
            return;
        }

        foreach (var operationBlock in context.OperationBlocks)
        {
            AnalyzeIntraProceduralNameRuleFlows(context, config, violations, callerTypeName, callerLayerName, callerMatch, operationBlock, context.OwningSymbol);
        }
    }

    public static void AnalyzeIntraProceduralLambdaNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
    {
        if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IAnonymousFunctionOperation anonymousFunction)
        {
            return;
        }

        var caller = BoundaryRules.LayerDependencies.LayerDependencyAnalyzer.TryGetCallerLayer(context, config, context.Node);
        if (caller is null)
        {
            return;
        }

        AnalyzeIntraProceduralNameRuleFlows(context, config, violations, caller.Value.TypeName, caller.Value.Match.Layer.Name, caller.Value.Match, anonymousFunction.Body, anonymousFunction.Symbol);
    }

    private static void AnalyzeIntraProceduralNameRuleFlows(OperationBlockAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, Core.LayerModel.LayerMatch callerMatch, IOperation operationBlock, ISymbol owningSymbol)
    {
        if (!ControlFlowGraphFactory.TryCreate(operationBlock, out var graph))
        {
            return;
        }

        var flows = NameRuleIntraProceduralFlowAnalyzer.Analyze(graph, owningSymbol);
        var evaluations = EvaluateIntraProceduralNameRuleFlows(config, callerMatch, flows);
        foreach (var evaluation in evaluations)
        {
            ReportNameRuleViolation(context, violations, callerTypeName, callerLayerName, evaluation.Violation, evaluation.Location);
        }
    }

    private static void AnalyzeIntraProceduralNameRuleFlows(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, string callerTypeName, string callerLayerName, Core.LayerModel.LayerMatch callerMatch, IOperation operationBlock, ISymbol owningSymbol)
    {
        var flows = NameRuleIntraProceduralFlowAnalyzer.AnalyzeLinearly(operationBlock, owningSymbol);
        var evaluations = EvaluateIntraProceduralNameRuleFlows(config, callerMatch, flows);
        foreach (var evaluation in evaluations)
        {
            ReportNameRuleViolation(context, violations, callerTypeName, callerLayerName, evaluation.Violation, evaluation.Location);
        }
    }

    private static ImmutableArray<NameRuleFlowViolation> EvaluateIntraProceduralNameRuleFlows(AnalyzerConfig config, Core.LayerModel.LayerMatch callerMatch, ImmutableArray<NameRuleProvenanceFlow> flows)
    {
        var evaluations = ImmutableArray.CreateBuilder<NameRuleFlowViolation>();
        foreach (var flow in flows)
        {
            if (flow.ImmediateSource is { } immediateSource
                && config.Engine.EvaluateNameRules(callerMatch, NameRuleTrigger.ValueMovement, immediateSource, flow.Target, flow.Site, NameRuleValueTrackingMode.IntraProcedural) is not null)
            {
                continue;
            }

            var violation = config.Engine.EvaluateNameRules(callerMatch, NameRuleTrigger.ValueMovement, flow.Source, flow.Target, flow.Site, NameRuleValueTrackingMode.IntraProcedural);
            if (violation is null)
            {
                continue;
            }

            evaluations.Add(new NameRuleFlowViolation(violation.Value, flow.Location));
        }

        var result = evaluations.ToImmutable();

        return result;
    }

    private static bool TryGetOperationBlockCaller(OperationBlockAnalysisContext context, AnalyzerConfig config, out string callerTypeName, out string callerLayerName, out Core.LayerModel.LayerMatch callerMatch)
    {
        var containingType = context.OwningSymbol.ContainingType;
        if (containingType is null)
        {
            callerTypeName = string.Empty;
            callerLayerName = string.Empty;
            callerMatch = default;
            var missingTypeResult = false;

            return missingTypeResult;
        }

        var namespaceName = containingType.ContainingNamespace.IsGlobalNamespace ? string.Empty : containingType.ContainingNamespace.ToDisplayString();
        var layer = config.FindLayer(containingType.Name, namespaceName, containingType);
        if (layer is null)
        {
            callerTypeName = string.Empty;
            callerLayerName = string.Empty;
            callerMatch = default;
            var missingLayerResult = false;

            return missingLayerResult;
        }

        callerTypeName = containingType.Name;
        callerLayerName = layer.Value.Layer.Name;
        callerMatch = layer.Value;
        var result = true;

        return result;
    }

    private readonly struct NameRuleFlowViolation(NameRuleViolation violation, Location location)
    {
        public NameRuleViolation Violation { get; } = violation;
        public Location Location { get; } = location;
    }
}