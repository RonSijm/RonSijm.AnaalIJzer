using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.OperationPolicies.Policies;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Analysis;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.Operations;

internal static class ForbiddenOperationPolicyAnalyzer
{
	internal static void AnalyzeOperation(OperationAnalysisContext context, AnalyzerConfig config)
	{
		if (!SemanticOperationFactory.TryCreate(context.Operation, context.ContainingSymbol, out var operation))
		{
			return;
		}

		var callerType = operation.CallerSymbol?.ContainingType;
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

		var evaluation = config.EvaluateForbiddenOperationPolicies(callerMatch.Value, operation);
		if (evaluation is null)
		{
			return;
		}

		Report(context, callerType, callerMatch.Value.Layer.Name, operation, evaluation.Value);
	}

	private static void Report(OperationAnalysisContext context, INamedTypeSymbol callerType, string callerLayerName, SemanticOperation operation, ForbiddenOperationPolicyEvaluation evaluation)
	{
		var rule = evaluation.Rule;
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, callerType.Name)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, callerLayerName)
			.Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, operation.CallerSymbol?.Name ?? callerType.Name)
			.Add(ArchitecturalDiagnostics.PropertyDeclarationTarget, operation.Kind.ToString())
			.Add(ArchitecturalDiagnostics.PropertySite, operation.Site)
			.Add(ArchitecturalDiagnostics.PropertyOperationKind, operation.Kind.ToString())
			.Add(ArchitecturalDiagnostics.PropertyOperationDisplayName, operation.DisplayName)
			.Add(ArchitecturalDiagnostics.PropertyOperationPolicyRule, rule.DisplayName)
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
			.Add(ArchitecturalDiagnostics.PropertyComment, rule.Description ?? evaluation.Policy.Description)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, rule.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

		context.ReportDiagnostic(ArchitecturalDiagnostics.CreateDiagnostic(
			ArchitecturalDiagnostics.OperationNotAllowed,
			operation.Location,
			properties,
			callerType.Name,
			callerLayerName,
			operation.DisplayName,
			operation.Site,
			evaluation.Reason));
	}
}
