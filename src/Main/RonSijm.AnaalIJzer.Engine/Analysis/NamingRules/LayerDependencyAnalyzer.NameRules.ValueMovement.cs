using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.NameRules;
using RonSijm.AnaalIJzer.Core.Violations;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.NamingRules;

public static partial class LayerDependencyAnalyzer
{
    public static void AnalyzeInvocationNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, InvocationExpressionSyntax invocation)
	{
		if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
		{
			return;
		}

		AnalyzeArgumentNameRules(context, config, violations, invocation.ArgumentList, method, DependencySites.Method);
	}

    public static void AnalyzeObjectCreationNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ExpressionSyntax objectCreation)
	{
		if (context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken).Symbol is not IMethodSymbol constructor)
		{
			return;
		}

		var argumentList = objectCreation switch
		{
			ObjectCreationExpressionSyntax explicitCreation => explicitCreation.ArgumentList,
			ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.ArgumentList,
			_ => null
		};
		if (argumentList is null)
		{
			return;
		}

		AnalyzeArgumentNameRules(context, config, violations, argumentList, constructor, DependencySites.Constructor);
	}

	private static void AnalyzeArgumentNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ArgumentListSyntax argumentList, IMethodSymbol method, string site)
	{
		for (var i = 0; i < argumentList.Arguments.Count; i++)
		{
			var argument = argumentList.Arguments[i];
			var parameter = NameRuleSemanticSubjectResolver.FindParameter(method, argument, i);
			if (parameter is null)
			{
				continue;
			}

			var source = NameRuleSemanticSubjectResolver.CreateExpressionSubject(argument.Expression, context.SemanticModel, context.CancellationToken);
			var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(parameter, argument.NameColon?.Name.Identifier.ValueText, preferContainingType: false);
			if (source is null || target is null)
			{
				continue;
			}

			AnalyzeNameRulePair(context, config, violations, source.Value, target.Value, site, argument.Expression.GetLocation());
		}
	}

	internal static void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var assignment = (AssignmentExpressionSyntax)context.Node;
		var source = NameRuleSemanticSubjectResolver.CreateExpressionSubject(assignment.Right, context.SemanticModel, context.CancellationToken);
		var target = NameRuleSemanticSubjectResolver.CreateAssignmentTargetSubject(assignment.Left, context.SemanticModel, context.CancellationToken);
		if (source is null || target is null)
		{
			return;
		}

		var site = NameRuleSemanticSubjectResolver.GetAssignmentSite(assignment.Left, context.SemanticModel, context.CancellationToken);
		AnalyzeNameRulePair(context, config, violations, source.Value, target.Value, site, assignment.Right.GetLocation());
	}

    public static void AnalyzeLocalInitializerNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, LocalDeclarationStatementSyntax localDecl)
	{
		foreach (var variable in localDecl.Declaration.Variables)
		{
			if (variable.Initializer is null || context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not ILocalSymbol localSymbol)
			{
				continue;
			}

			var source = NameRuleSemanticSubjectResolver.CreateExpressionSubject(variable.Initializer.Value, context.SemanticModel, context.CancellationToken);
			var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(localSymbol, variable.Identifier.ValueText, preferContainingType: false);
			if (source is null || target is null)
			{
				continue;
			}

			AnalyzeNameRulePair(context, config, violations, source.Value, target.Value, DependencySites.Local, variable.Initializer.Value.GetLocation());
		}
	}

    public static void AnalyzeFieldInitializerNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, FieldDeclarationSyntax fieldDecl)
	{
		foreach (var variable in fieldDecl.Declaration.Variables)
		{
			if (variable.Initializer is null || context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not IFieldSymbol fieldSymbol)
			{
				continue;
			}

			var source = NameRuleSemanticSubjectResolver.CreateExpressionSubject(variable.Initializer.Value, context.SemanticModel, context.CancellationToken);
			var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(fieldSymbol, variable.Identifier.ValueText, preferContainingType: false);
			if (source is null || target is null)
			{
				continue;
			}

			AnalyzeNameRulePair(context, config, violations, source.Value, target.Value, DependencySites.Field, variable.Initializer.Value.GetLocation());
		}
	}

    public static void AnalyzePropertyInitializerNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, PropertyDeclarationSyntax propertyDecl)
	{
		if (propertyDecl.Initializer is null || context.SemanticModel.GetDeclaredSymbol(propertyDecl, context.CancellationToken) is not IPropertySymbol propertySymbol)
		{
			return;
		}

		var source = NameRuleSemanticSubjectResolver.CreateExpressionSubject(propertyDecl.Initializer.Value, context.SemanticModel, context.CancellationToken);
		var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(propertySymbol, propertyDecl.Identifier.ValueText, preferContainingType: false);
		if (source is null || target is null)
		{
			return;
		}

		AnalyzeNameRulePair(context, config, violations, source.Value, target.Value, DependencySites.Property, propertyDecl.Initializer.Value.GetLocation());
	}

	internal static void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var returnStatement = (ReturnStatementSyntax)context.Node;
		if (returnStatement.Expression is null)
		{
			return;
		}

		var method = returnStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
		if (method is null || context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken) is not IMethodSymbol methodSymbol)
		{
			return;
		}

		var source = NameRuleSemanticSubjectResolver.CreateExpressionSubject(returnStatement.Expression, context.SemanticModel, context.CancellationToken);
		var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(methodSymbol, method.Identifier.ValueText, preferContainingType: false);
		if (source is null || target is null)
		{
			return;
		}

		AnalyzeNameRulePair(context, config, violations, source.Value, target.Value, DependencySites.MethodReturn, returnStatement.Expression.GetLocation());
	}

	private static void AnalyzeNameRulePair(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, NameRuleSubject source, NameRuleSubject target, string site, Location reportLocation, NameRuleTrigger trigger = NameRuleTrigger.ValueMovement)
	{
		var caller = BoundaryRules.LayerDependencies.LayerDependencyAnalyzer.TryGetCallerLayer(context, config, context.Node);
		if (caller is null)
		{
			return;
		}

		var violation = config.Engine.EvaluateNameRules(caller.Value.Match, trigger, source, target, site);
		if (violation is null)
		{
			return;
		}

		ReportNameRuleViolation(context, violations, caller.Value.TypeName, caller.Value.Match.Layer.Name, violation.Value, reportLocation);
	}
}
