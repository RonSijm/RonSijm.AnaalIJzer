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

		var result = argumentIndex < method.Parameters.Length ? method.Parameters[argumentIndex] : null;

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
		var unwrapped = UnwrapExpression(expression);
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
		while (current is ParenthesizedExpressionSyntax parenthesized)
		{
			current = parenthesized.Expression;
		}

		if (current is CastExpressionSyntax cast)
		{
			current = cast.Expression;
		}

		return current;
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
