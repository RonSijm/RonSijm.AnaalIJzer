using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.NameRules;

public static class NameRuleSemanticSubjectResolver
{
	public static IParameterSymbol? FindParameter(IMethodSymbol method, ArgumentSyntax argument, int argumentIndex)
	{
		if (argument.NameColon?.Name.Identifier.ValueText is { } name)
		{
			var namedResult = method.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal));

			return namedResult;
		}

		var result = argumentIndex < method.Parameters.Length
			? method.Parameters[argumentIndex]
			: method.Parameters.LastOrDefault(parameter => parameter.IsParams);

		return result;
	}

	public static NameRuleSubject? CreateAssignmentTargetSubject(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		bool? preferContainingType = IsObjectInitializerAssignmentTarget(expression) ? true : null;
		var result = CreateExpressionSubject(expression, semanticModel, cancellationToken, preferContainingType);

		return result;
	}

	public static string GetAssignmentSite(ExpressionSyntax target, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var symbol = semanticModel.GetSymbolInfo(target, cancellationToken).Symbol;
		var result = symbol switch
		{
			IFieldSymbol => DependencySites.Field,
			IPropertySymbol => DependencySites.Property,
			ILocalSymbol => DependencySites.Local,
			IParameterSymbol => DependencySites.Method,
			_ => DependencySites.Local
		};

		return result;
	}

	public static NameRuleSubject? CreateExpressionSubject(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, bool? preferContainingTypeOverride = null)
	{
		var subjects = CreateExpressionSubjects(expression, semanticModel, cancellationToken, preferContainingTypeOverride);
		NameRuleSubject? result = subjects.IsDefaultOrEmpty ? null : subjects[0];

		return result;
	}

	public static ImmutableArray<NameRuleSubject> CreateExpressionSubjects(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, bool? preferContainingTypeOverride = null)
	{
		var subjects = ImmutableArray.CreateBuilder<NameRuleSubject>();
		AddExpressionSubjects(subjects, expression, semanticModel, cancellationToken, preferContainingTypeOverride);
		var result = subjects.ToImmutable();

		return result;
	}

	public static NameRuleSubject? CreateDeclarationTargetSubject(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var result = CreateDeclarationTargetSubject(declaration.Designation, semanticModel, cancellationToken);

		return result;
	}

	public static NameRuleSubject? CreateDeclarationTargetSubject(VariableDesignationSyntax designation, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		_ = semanticModel;
		_ = cancellationToken;
		var name = designation is SingleVariableDesignationSyntax singleVariable
			? singleVariable.Identifier.ValueText
			: string.Empty;
		NameRuleSubject? result = string.IsNullOrWhiteSpace(name)
			? null
			: new NameRuleSubject(name, [name], symbol: null);

		return result;
	}

	private static void AddExpressionSubjects(ImmutableArray<NameRuleSubject>.Builder subjects, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, bool? preferContainingTypeOverride)
	{
		var unwrapped = UnwrapExpression(expression);
		switch (unwrapped)
		{
			case DeclarationExpressionSyntax declaration:
				var declarationSubject = CreateDeclarationTargetSubject(declaration, semanticModel, cancellationToken);
				if (declarationSubject is not null)
				{
					subjects.Add(declarationSubject.Value);
				}

				return;
			case ConditionalExpressionSyntax conditional:
				AddExpressionSubjects(subjects, conditional.WhenTrue, semanticModel, cancellationToken, preferContainingTypeOverride);
				AddExpressionSubjects(subjects, conditional.WhenFalse, semanticModel, cancellationToken, preferContainingTypeOverride);
				return;
			case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } coalesce:
				AddExpressionSubjects(subjects, coalesce.Left, semanticModel, cancellationToken, preferContainingTypeOverride);
				AddExpressionSubjects(subjects, coalesce.Right, semanticModel, cancellationToken, preferContainingTypeOverride);
				return;
			case TupleExpressionSyntax tuple:
				foreach (var argument in tuple.Arguments)
				{
					AddExpressionSubjects(subjects, argument.Expression, semanticModel, cancellationToken, preferContainingTypeOverride);
				}

				return;
		}

		var subject = CreateSingleExpressionSubject(unwrapped, semanticModel, cancellationToken, preferContainingTypeOverride);
		if (subject is null || subjects.Any(existing => string.Equals(existing.DisplayName, subject.Value.DisplayName, StringComparison.Ordinal) && SymbolEqualityComparer.Default.Equals(existing.Symbol, subject.Value.Symbol)))
		{
			return;
		}

		subjects.Add(subject.Value);
	}

	private static NameRuleSubject? CreateSingleExpressionSubject(ExpressionSyntax unwrapped, SemanticModel semanticModel, CancellationToken cancellationToken, bool? preferContainingTypeOverride)
	{
		if (unwrapped.IsKind(SyntaxKind.NullLiteralExpression)
		    || unwrapped.IsKind(SyntaxKind.NumericLiteralExpression)
		    || unwrapped.IsKind(SyntaxKind.StringLiteralExpression)
		    || unwrapped.IsKind(SyntaxKind.TrueLiteralExpression)
		    || unwrapped.IsKind(SyntaxKind.FalseLiteralExpression))
		{
			return null;
		}

		var symbol = semanticModel.GetSymbolInfo(unwrapped, cancellationToken).Symbol;
		var type = semanticModel.GetTypeInfo(unwrapped, cancellationToken).Type ?? GetSymbolValueType(symbol);
		if (symbol is null)
		{
			var expressionName = unwrapped.ToString();
			var expressionResult = new NameRuleSubject(expressionName, [expressionName], type);

			return expressionResult;
		}

		var preferContainingType = preferContainingTypeOverride ?? (unwrapped is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression is not ThisExpressionSyntax and not BaseExpressionSyntax);
		var result = CreateSymbolSubject(symbol, unwrapped.ToString(), preferContainingType, type);

		return result;
	}

	public static NameRuleSubject? CreateSymbolSubject(ISymbol symbol, string? syntaxName, bool preferContainingType, ITypeSymbol? explicitType = null)
	{
		var symbolName = symbol.Name;
		if (string.IsNullOrWhiteSpace(symbolName))
		{
			return null;
		}

		var type = explicitType ?? GetSymbolValueType(symbol);
		var containingTypeName = symbol.ContainingType?.Name;
		var displayName = preferContainingType && !string.IsNullOrWhiteSpace(containingTypeName)
			? containingTypeName + "." + symbolName
			: symbolName;
		var candidates = ImmutableArray.CreateBuilder<string>();
		AddCandidate(candidates, displayName);
		AddCandidate(candidates, symbolName);
		if (!string.IsNullOrWhiteSpace(syntaxName))
		{
			AddCandidate(candidates, syntaxName!);
		}

		if (!string.IsNullOrWhiteSpace(containingTypeName))
		{
			AddCandidate(candidates, containingTypeName + "." + symbolName);
		}

		var result = new NameRuleSubject(displayName, candidates.ToImmutable(), type);

		return result;
	}

	private static bool IsObjectInitializerAssignmentTarget(ExpressionSyntax expression)
	{
		var result = expression.Parent is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax { Parent: ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax } };

		return result;
	}

	private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
	{
		var current = expression;
		while (true)
		{
			switch (current)
			{
				case ParenthesizedExpressionSyntax parenthesized:
					current = parenthesized.Expression;
					continue;
				case CastExpressionSyntax cast:
					current = cast.Expression;
					continue;
				case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } asExpression:
					current = asExpression.Left;
					continue;
				case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressNullableWarning:
					current = suppressNullableWarning.Operand;
					continue;
				default:
					var result = current;

					return result;
			}
		}
	}

	private static ITypeSymbol? GetSymbolValueType(ISymbol? symbol)
	{
		var result = symbol switch
		{
			ILocalSymbol local => local.Type,
			IParameterSymbol parameter => parameter.Type,
			IFieldSymbol field => field.Type,
			IPropertySymbol property => property.Type,
			IMethodSymbol method => method.ReturnType,
			IEventSymbol @event => @event.Type,
			ITypeSymbol type => type,
			_ => null
		};

		return result;
	}

	private static void AddCandidate(ImmutableArray<string>.Builder candidates, string value)
	{
		if (!candidates.Any(candidate => string.Equals(candidate, value, StringComparison.Ordinal)))
		{
			candidates.Add(value);
		}
	}
}
