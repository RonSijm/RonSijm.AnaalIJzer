using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Operations;

internal static class BehavioralOperationPolicyAnalyzer
{
    internal static void AnalyzeOperationBlock(OperationBlockAnalysisContext context, AnalyzerConfig config)
    {
        var callerType = context.OwningSymbol.ContainingType;
        if (callerType is null)
        {
            return;
        }

        var callerNamespace = callerType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : callerType.ContainingNamespace.ToDisplayString();
        var callerMatch = config.FindLayer(callerType.Name, callerNamespace, callerType);
        if (callerMatch is null || callerMatch.Value.Layer.IsForbidden)
        {
            return;
        }

        foreach (var operationBlock in context.OperationBlocks)
        {
            if (!BehavioralOperationBodyAnalysis.TryCreate(operationBlock, context.OwningSymbol, out var body))
            {
                continue;
            }

            var evaluations = config.EvaluateBehavioralOperationPolicies(callerMatch.Value, body);
            foreach (var evaluation in evaluations)
            {
                Report(context, callerType, callerMatch.Value.Layer.Name, body, evaluation);
            }
        }
    }

    private static void Report(OperationBlockAnalysisContext context, INamedTypeSymbol callerType, string callerLayerName, BehavioralOperationBodyAnalysis body, BehavioralOperationPolicyEvaluation evaluation)
    {
        var descriptor = GetDescriptor(evaluation.ViolationKind);
        var occurrence = evaluation.Occurrence;
        var operation = occurrence?.Operation;
        var location = operation?.Location ?? body.DeclarationLocation;
        var site = operation?.Site ?? "Declaration";
        var operationKind = operation?.Kind.ToString() ?? "Declaration";
        var operationDisplayName = operation?.DisplayName ?? evaluation.Rule.DisplayName;
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add(ArchitecturalDiagnostics.PropertyCallerTypeName, callerType.Name)
            .Add(ArchitecturalDiagnostics.PropertyCallerLayerName, callerLayerName)
            .Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, body.OwningSymbol.Name)
            .Add(ArchitecturalDiagnostics.PropertyDeclarationTarget, evaluation.ViolationKind.ToString())
            .Add(ArchitecturalDiagnostics.PropertySite, site)
            .Add(ArchitecturalDiagnostics.PropertyOperationKind, operationKind)
            .Add(ArchitecturalDiagnostics.PropertyOperationDisplayName, operationDisplayName)
            .Add(ArchitecturalDiagnostics.PropertyOperationPolicyRule, evaluation.Rule.DisplayName)
            .Add(ArchitecturalDiagnostics.PropertyBehavioralOperationViolationKind, evaluation.ViolationKind.ToString())
            .Add(ArchitecturalDiagnostics.PropertyBehavioralOperationOrdering, evaluation.Rule.Ordering.ToString())
            .Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
            .Add(ArchitecturalDiagnostics.PropertyComment, evaluation.Rule.Description ?? evaluation.Policy.Description)
            .Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, evaluation.Rule.XmlPath)
            .Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, evaluation.Rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, evaluation.Rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

        context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
            descriptor,
            location,
            properties,
            callerType.Name,
            callerLayerName,
            evaluation.Rule.DisplayName,
            site,
            evaluation.Reason));
    }

    private static DiagnosticDescriptor GetDescriptor(BehavioralOperationViolationKind violationKind)
    {
        var result = violationKind switch
        {
            BehavioralOperationViolationKind.MissingRequiredOperation or BehavioralOperationViolationKind.RequiredOperationDoesNotDominateExit => ArchitecturalDiagnostics.OperationRequiredMissing,
            BehavioralOperationViolationKind.MaximumOperationCountExceeded => ArchitecturalDiagnostics.OperationCardinality,
            _ => ArchitecturalDiagnostics.OperationOrdering
        };

        return result;
    }
}