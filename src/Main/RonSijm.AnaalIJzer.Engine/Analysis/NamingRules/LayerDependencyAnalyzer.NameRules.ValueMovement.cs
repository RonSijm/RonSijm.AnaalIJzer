using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
			_ => null,
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

			if (argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
			{
				var parameterSource = NameRuleSemanticSubjectResolver.CreateSymbolSubject(parameter, parameter.Name, preferContainingType: false);
				var outTargets = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(argument.Expression, context.SemanticModel, context.CancellationToken);
				if (parameterSource is null)
				{
					continue;
				}

				foreach (var outTarget in outTargets)
				{
					AnalyzeNameRulePair(context, config, violations, parameterSource.Value, outTarget, site, argument.Expression.GetLocation());
				}

				continue;
			}

			var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(argument.Expression, context.SemanticModel, context.CancellationToken);
			var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(parameter, parameter.Name, preferContainingType: false);
			if (target is null)
			{
				continue;
			}

			AnalyzeNameRulePairs(context, config, violations, sources, target.Value, site, argument.Expression.GetLocation());
		}
	}

	internal static void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var assignment = (AssignmentExpressionSyntax)context.Node;
		if (IsDeconstructionTarget(assignment.Left))
		{
			AnalyzeDeconstructionAssignment(context, config, violations, assignment.Left, assignment.Right);
			return;
		}

		var target = NameRuleSemanticSubjectResolver.CreateAssignmentTargetSubject(assignment.Left, context.SemanticModel, context.CancellationToken);
		if (target is null)
		{
			return;
		}

		var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(assignment.Right, context.SemanticModel, context.CancellationToken);
		var site = NameRuleSemanticSubjectResolver.GetAssignmentSite(assignment.Left, context.SemanticModel, context.CancellationToken);
		AnalyzeNameRulePairs(context, config, violations, sources, target.Value, site, assignment.Right.GetLocation());
	}

	public static void AnalyzeLocalInitializerNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, LocalDeclarationStatementSyntax localDecl)
	{
		foreach (var variable in localDecl.Declaration.Variables)
		{
			if (variable.Initializer is null || context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not ILocalSymbol localSymbol)
			{
				continue;
			}

			var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(localSymbol, variable.Identifier.ValueText, preferContainingType: false);
			if (target is null)
			{
				continue;
			}

			var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(variable.Initializer.Value, context.SemanticModel, context.CancellationToken);
			AnalyzeNameRulePairs(context, config, violations, sources, target.Value, DependencySites.Local, variable.Initializer.Value.GetLocation());
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

			var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(fieldSymbol, variable.Identifier.ValueText, preferContainingType: false);
			if (target is null)
			{
				continue;
			}

			var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(variable.Initializer.Value, context.SemanticModel, context.CancellationToken);
			AnalyzeNameRulePairs(context, config, violations, sources, target.Value, DependencySites.Field, variable.Initializer.Value.GetLocation());
		}
	}

	public static void AnalyzePropertyInitializerNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, PropertyDeclarationSyntax propertyDecl)
	{
		if (propertyDecl.Initializer is null || context.SemanticModel.GetDeclaredSymbol(propertyDecl, context.CancellationToken) is not IPropertySymbol propertySymbol)
		{
			return;
		}

		var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(propertySymbol, propertyDecl.Identifier.ValueText, preferContainingType: false);
		if (target is null)
		{
			return;
		}

		var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(propertyDecl.Initializer.Value, context.SemanticModel, context.CancellationToken);
		AnalyzeNameRulePairs(context, config, violations, sources, target.Value, DependencySites.Property, propertyDecl.Initializer.Value.GetLocation());
	}

	internal static void AnalyzeReturnStatement(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var returnStatement = (ReturnStatementSyntax)context.Node;
		if (returnStatement.Expression is null || !TryGetReturnTarget(returnStatement, context.SemanticModel, context.CancellationToken, out var target, out var site))
		{
			return;
		}

		var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(returnStatement.Expression, context.SemanticModel, context.CancellationToken);
		AnalyzeNameRulePairs(context, config, violations, sources, target, site, returnStatement.Expression.GetLocation());
	}

	internal static void AnalyzeArrowExpressionNameRules(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations)
	{
		var arrow = (ArrowExpressionClauseSyntax)context.Node;
		if (!TryGetArrowTarget(arrow, context.SemanticModel, context.CancellationToken, out var target, out var site))
		{
			return;
		}

		var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(arrow.Expression, context.SemanticModel, context.CancellationToken);
		AnalyzeNameRulePairs(context, config, violations, sources, target, site, arrow.Expression.GetLocation());
	}

	private static void AnalyzeDeconstructionAssignment(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ExpressionSyntax targetExpression, ExpressionSyntax sourceExpression)
	{
		if (!TryGetDeconstructionTargets(targetExpression, out var targets) || sourceExpression is not TupleExpressionSyntax sourceTuple || targets.Length != sourceTuple.Arguments.Count)
		{
			return;
		}

		for (var i = 0; i < targets.Length; i++)
		{
			AnalyzeDeconstructionComponent(context, config, violations, targets[i], sourceTuple.Arguments[i].Expression);
		}
	}

	private static void AnalyzeDeconstructionComponent(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, SyntaxNode targetNode, ExpressionSyntax sourceExpression)
	{
		if (TryGetNestedDeconstructionTargets(targetNode, out var nestedTargets))
		{
			if (sourceExpression is not TupleExpressionSyntax sourceTuple || nestedTargets.Length != sourceTuple.Arguments.Count)
			{
				return;
			}

			for (var i = 0; i < nestedTargets.Length; i++)
			{
				AnalyzeDeconstructionComponent(context, config, violations, nestedTargets[i], sourceTuple.Arguments[i].Expression);
			}

			return;
		}

		var target = targetNode switch
		{
			DeclarationExpressionSyntax declaration => NameRuleSemanticSubjectResolver.CreateDeclarationTargetSubject(declaration, context.SemanticModel, context.CancellationToken),
			VariableDesignationSyntax designation => NameRuleSemanticSubjectResolver.CreateDeclarationTargetSubject(designation, context.SemanticModel, context.CancellationToken),
			ExpressionSyntax expression => NameRuleSemanticSubjectResolver.CreateAssignmentTargetSubject(expression, context.SemanticModel, context.CancellationToken),
			_ => null,
		};
		if (target is null)
		{
			return;
		}

		var site = targetNode is VariableDesignationSyntax or DeclarationExpressionSyntax
			? DependencySites.Local
			: NameRuleSemanticSubjectResolver.GetAssignmentSite((ExpressionSyntax)targetNode, context.SemanticModel, context.CancellationToken);
		var sources = NameRuleSemanticSubjectResolver.CreateExpressionSubjects(sourceExpression, context.SemanticModel, context.CancellationToken);
		AnalyzeNameRulePairs(context, config, violations, sources, target.Value, site, sourceExpression.GetLocation());
	}

	private static bool IsDeconstructionTarget(ExpressionSyntax expression)
	{
		var result = expression is TupleExpressionSyntax or DeclarationExpressionSyntax { Designation: ParenthesizedVariableDesignationSyntax };

		return result;
	}

	private static bool TryGetDeconstructionTargets(ExpressionSyntax expression, out ImmutableArray<SyntaxNode> targets)
	{
		var result = expression switch
		{
			TupleExpressionSyntax tuple => tuple.Arguments.Select(argument => (SyntaxNode)argument.Expression).ToImmutableArray(),
			DeclarationExpressionSyntax { Designation: ParenthesizedVariableDesignationSyntax designation } => designation.Variables.Cast<SyntaxNode>().ToImmutableArray(),
			_ => ImmutableArray<SyntaxNode>.Empty,
		};

		targets = result;
		return !result.IsDefaultOrEmpty;
	}

	private static bool TryGetNestedDeconstructionTargets(SyntaxNode node, out ImmutableArray<SyntaxNode> targets)
	{
		var result = node switch
		{
			TupleExpressionSyntax tuple => tuple.Arguments.Select(argument => (SyntaxNode)argument.Expression).ToImmutableArray(),
			DeclarationExpressionSyntax { Designation: ParenthesizedVariableDesignationSyntax designation } => designation.Variables.Cast<SyntaxNode>().ToImmutableArray(),
			ParenthesizedVariableDesignationSyntax designation => designation.Variables.Cast<SyntaxNode>().ToImmutableArray(),
			_ => ImmutableArray<SyntaxNode>.Empty,
		};

		targets = result;
		return !result.IsDefaultOrEmpty;
	}

	private static bool TryGetReturnTarget(ReturnStatementSyntax returnStatement, SemanticModel semanticModel, CancellationToken cancellationToken, out NameRuleSubject target, out string site)
	{
		foreach (var ancestor in returnStatement.Ancestors())
		{
			switch (ancestor)
			{
				case AnonymousFunctionExpressionSyntax:
					target = default;
					site = string.Empty;
					var lambdaResult = false;

					return lambdaResult;
				case LocalFunctionStatementSyntax localFunction when semanticModel.GetDeclaredSymbol(localFunction, cancellationToken) is IMethodSymbol localFunctionSymbol:
					if (!TryCreateMethodTarget(localFunctionSymbol, localFunction.Identifier.ValueText, out target))
					{
						site = string.Empty;
						var localFunctionMissingResult = false;

						return localFunctionMissingResult;
					}

					site = DependencySites.MethodReturn;
					var localFunctionResult = true;

					return localFunctionResult;
				case MethodDeclarationSyntax method when semanticModel.GetDeclaredSymbol(method, cancellationToken) is IMethodSymbol methodSymbol:
					if (!TryCreateMethodTarget(methodSymbol, method.Identifier.ValueText, out target))
					{
						site = string.Empty;
						var methodMissingResult = false;

						return methodMissingResult;
					}

					site = DependencySites.MethodReturn;
					var methodResult = true;

					return methodResult;
				case AccessorDeclarationSyntax accessor when accessor.IsKind(SyntaxKind.GetAccessorDeclaration) && TryGetAccessorPropertyTarget(accessor, semanticModel, cancellationToken, out target):
					site = DependencySites.Property;
					var accessorResult = true;

					return accessorResult;
			}
		}

		target = default;
		site = string.Empty;
		var result = false;

		return result;
	}

	private static bool TryGetArrowTarget(ArrowExpressionClauseSyntax arrow, SemanticModel semanticModel, CancellationToken cancellationToken, out NameRuleSubject target, out string site)
	{
		switch (arrow.Parent)
		{
			case AnonymousFunctionExpressionSyntax:
				target = default;
				site = string.Empty;
				var lambdaResult = false;

				return lambdaResult;
			case LocalFunctionStatementSyntax localFunction when semanticModel.GetDeclaredSymbol(localFunction, cancellationToken) is IMethodSymbol localFunctionSymbol:
				if (!TryCreateMethodTarget(localFunctionSymbol, localFunction.Identifier.ValueText, out target))
				{
					site = string.Empty;
					var localFunctionMissingResult = false;

					return localFunctionMissingResult;
				}

				site = DependencySites.MethodReturn;
				var localFunctionResult = true;

				return localFunctionResult;
			case MethodDeclarationSyntax method when semanticModel.GetDeclaredSymbol(method, cancellationToken) is IMethodSymbol methodSymbol:
				if (!TryCreateMethodTarget(methodSymbol, method.Identifier.ValueText, out target))
				{
					site = string.Empty;
					var methodMissingResult = false;

					return methodMissingResult;
				}

				site = DependencySites.MethodReturn;
				var methodResult = true;

				return methodResult;
			case PropertyDeclarationSyntax property when semanticModel.GetDeclaredSymbol(property, cancellationToken) is IPropertySymbol propertySymbol:
				if (!TryCreatePropertyTarget(propertySymbol, property.Identifier.ValueText, out target))
				{
					site = string.Empty;
					var propertyMissingResult = false;

					return propertyMissingResult;
				}

				site = DependencySites.Property;
				var propertyResult = true;

				return propertyResult;
			case IndexerDeclarationSyntax indexer when semanticModel.GetDeclaredSymbol(indexer, cancellationToken) is IPropertySymbol indexerSymbol:
				if (!TryCreatePropertyTarget(indexerSymbol, indexerSymbol.Name, out target))
				{
					site = string.Empty;
					var indexerMissingResult = false;

					return indexerMissingResult;
				}

				site = DependencySites.Property;
				var indexerResult = true;

				return indexerResult;
			case AccessorDeclarationSyntax accessor when accessor.IsKind(SyntaxKind.GetAccessorDeclaration) && TryGetAccessorPropertyTarget(accessor, semanticModel, cancellationToken, out target):
				site = DependencySites.Property;
				var accessorResult = true;

				return accessorResult;
			default:
				target = default;
				site = string.Empty;
				var result = false;

				return result;
		}
	}

	private static bool TryGetAccessorPropertyTarget(AccessorDeclarationSyntax accessor, SemanticModel semanticModel, CancellationToken cancellationToken, out NameRuleSubject target)
	{
		var owner = accessor.Parent?.Parent;
		var propertySymbol = owner is null ? null : semanticModel.GetDeclaredSymbol(owner, cancellationToken) as IPropertySymbol;
		if (propertySymbol is null || !TryCreatePropertyTarget(propertySymbol, propertySymbol.Name, out target))
		{
			target = default;
			var missingResult = false;

			return missingResult;
		}

		var result = true;

		return result;
	}

	private static bool TryCreateMethodTarget(IMethodSymbol method, string syntaxName, out NameRuleSubject target)
	{
		var subject = NameRuleSemanticSubjectResolver.CreateSymbolSubject(method, syntaxName, preferContainingType: false);
		if (subject is null)
		{
			target = default;
			var missingResult = false;

			return missingResult;
		}

		target = subject.Value;
		var result = true;

		return result;
	}

	private static bool TryCreatePropertyTarget(IPropertySymbol property, string syntaxName, out NameRuleSubject target)
	{
		var subject = NameRuleSemanticSubjectResolver.CreateSymbolSubject(property, syntaxName, preferContainingType: false);
		if (subject is null)
		{
			target = default;
			var missingResult = false;

			return missingResult;
		}

		target = subject.Value;
		var result = true;

		return result;
	}

	private static void AnalyzeNameRulePairs(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, ImmutableArray<NameRuleSubject> sources, NameRuleSubject target, string site, Location reportLocation)
	{
		foreach (var source in sources)
		{
			AnalyzeNameRulePair(context, config, violations, source, target, site, reportLocation);
		}
	}

	private static void AnalyzeNameRulePair(SyntaxNodeAnalysisContext context, AnalyzerConfig config, ConcurrentBag<ViolationRecord> violations, NameRuleSubject source, NameRuleSubject target, string site, Location reportLocation, NameRuleTrigger trigger = NameRuleTrigger.ValueMovement, NameRuleValueTrackingMode? valueTracking = null)
	{
		var caller = BoundaryRules.LayerDependencies.LayerDependencyAnalyzer.TryGetCallerLayer(context, config, context.Node);
		if (caller is null)
		{
			return;
		}

		var violation = config.Engine.EvaluateNameRules(caller.Value.Match, trigger, source, target, site, valueTracking);
		if (violation is null)
		{
			return;
		}

		ReportNameRuleViolation(context, violations, caller.Value.TypeName, caller.Value.Match.Layer.Name, violation.Value, reportLocation);
	}
}
