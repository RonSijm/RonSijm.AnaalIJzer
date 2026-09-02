using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.ReturnValues.Policies;
using RonSijm.AnaalIJzer.Diagnostics;
using RonSijm.AnaalIJzer.Engine.Analysis.BoundaryRules.LayerDependencies;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.TypePolicies.ReturnValues;

internal static class ReturnValuePolicyAnalyzer
{
	internal static void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context, AnalyzerConfig config)
	{
		var returnStatement = (ReturnStatementSyntax)context.Node;
		if (returnStatement.Expression is null)
		{
			return;
		}

		AnalyzeReturnExpression(context, config, returnStatement, returnStatement.Expression);
	}

	internal static void AnalyzeArrowExpressionClause(SyntaxNodeAnalysisContext context, AnalyzerConfig config)
	{
		var arrow = (ArrowExpressionClauseSyntax)context.Node;
		if (arrow.Parent is not MethodDeclarationSyntax and not LocalFunctionStatementSyntax)
		{
			return;
		}

		AnalyzeReturnExpression(context, config, arrow, arrow.Expression);
	}

	private static void AnalyzeReturnExpression(SyntaxNodeAnalysisContext context, AnalyzerConfig config, SyntaxNode returnSite, ExpressionSyntax expression)
	{
		if (!TryGetReturningMethod(context, expression, out var method))
		{
			return;
		}

		var caller = LayerDependencyAnalyzer.TryGetCallerLayer(context, config, returnSite);
		if (caller is null)
		{
			return;
		}

		var directExpression = UnwrapNonHandlingExpression(expression);
		var evaluation = config.Engine.EvaluateReturnValuePolicies(caller.Value.Match, directExpression, context.SemanticModel, context.CancellationToken);
		if (evaluation is null)
		{
			return;
		}

		Report(context, caller.Value, method, expression.GetLocation(), evaluation.Value);
	}

	private static bool TryGetReturningMethod(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, out IMethodSymbol method)
	{
		var enclosingSymbol = context.SemanticModel.GetEnclosingSymbol(expression.SpanStart, context.CancellationToken) as IMethodSymbol;
		if (enclosingSymbol is null || enclosingSymbol.MethodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction))
		{
			method = null!;
			return false;
		}

		method = enclosingSymbol;
		return true;
	}

	private static ExpressionSyntax UnwrapNonHandlingExpression(ExpressionSyntax expression)
	{
		var current = expression;
		while (true)
		{
			var next = current switch
			{
				ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
				CastExpressionSyntax cast => cast.Expression,
				AwaitExpressionSyntax awaitExpression => awaitExpression.Expression,
				BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AsExpression) => binary.Left,
				PostfixUnaryExpressionSyntax postfix when postfix.OperatorToken.IsKind(SyntaxKind.ExclamationToken) => postfix.Operand,
				_ => null
			};
			if (next is null)
			{
				break;
			}

			current = next;
		}

		var result = current;

		return result;
	}

	private static void Report(SyntaxNodeAnalysisContext context, (string TypeName, LayerMatch Match) caller, IMethodSymbol method, Location location, ReturnValuePolicyEvaluation evaluation)
	{
		var rule = evaluation.Rule;
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, caller.TypeName)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, caller.Match.Layer.Name)
			.Add(ArchitecturalDiagnostics.PropertyDeclaredSymbolName, method.Name)
			.Add(ArchitecturalDiagnostics.PropertyDeclarationTarget, DependencySites.MethodReturn)
			.Add(ArchitecturalDiagnostics.PropertySite, DependencySites.MethodReturn)
			.Add(ArchitecturalDiagnostics.PropertyReturnValueRuleTarget, rule.Matcher.Target.ToString())
			.Add(ArchitecturalDiagnostics.PropertyReturnValueRule, rule.DisplayName)
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, evaluation.Reason)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, rule.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, rule.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, rule.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.ReturnValuePolicyViolation,
			location,
			properties,
			method.Name,
			caller.Match.Layer.Name,
			DependencySites.MethodReturn,
			evaluation.Reason));
	}
}
