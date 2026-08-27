using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.Core.Matchers.Observations;

internal static class CodeObservationMatcherEvaluator
{
	internal static bool MatchesAll(ISymbol declarationSymbol, ImmutableArray<CodeObservationMatcher> matchers)
	{
		if (matchers.IsDefaultOrEmpty)
		{
			return true;
		}

		foreach (var matcher in matchers)
		{
			if (!MatchesAny(declarationSymbol, matcher))
			{
				return false;
			}
		}

		var result = true;

		return result;
	}

	private static bool MatchesAny(ISymbol declarationSymbol, CodeObservationMatcher matcher)
	{
		foreach (var syntaxReference in declarationSymbol.DeclaringSyntaxReferences)
		{
			var declarationSyntax = syntaxReference.GetSyntax();
			foreach (var node in GetCandidateNodes(declarationSyntax, matcher.Target))
			{
				if (MatchesAllConditions(node, matcher.Conditions))
				{
					return true;
				}
			}
		}

		var result = false;

		return result;
	}

	private static bool MatchesAllConditions(SyntaxNode node, ImmutableArray<MatchCondition> conditions)
	{
		var context = CreateContext(node);
		foreach (var condition in conditions)
		{
			if (!condition.Matches(context))
			{
				return false;
			}
		}

		var result = true;

		return result;
	}

	private static IEnumerable<SyntaxNode> GetCandidateNodes(SyntaxNode declarationSyntax, CodeObservationMatchTarget target)
	{
		var result = declarationSyntax
			.DescendantNodes(descendIntoChildren: static _ => true)
			.Where(node => MatchesTarget(node, target));

		return result;
	}

	private static bool MatchesTarget(SyntaxNode node, CodeObservationMatchTarget target)
	{
		var result = target switch
		{
			CodeObservationMatchTarget.Throw => node is ThrowStatementSyntax or ThrowExpressionSyntax,
			CodeObservationMatchTarget.Invocation => node is InvocationExpressionSyntax,
			CodeObservationMatchTarget.New => node is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax,
			CodeObservationMatchTarget.Identifier => node is IdentifierNameSyntax or GenericNameSyntax,
			CodeObservationMatchTarget.MemberAccess => node is MemberAccessExpressionSyntax,
			CodeObservationMatchTarget.Literal => node is LiteralExpressionSyntax,
			_ => false
		};

		return result;
	}

	private static MatchContext CreateContext(SyntaxNode node)
	{
		var (subjectName, associatedTypeName, associatedTypeNamespace) = GetObservationValues(node);
		var result = new MatchContext(subjectName, string.Empty, null, associatedTypeName, associatedTypeNamespace, null);

		return result;
	}

	private static (string SubjectName, string? AssociatedTypeName, string? AssociatedTypeNamespace) GetObservationValues(SyntaxNode node)
	{
		var result = node switch
		{
			ThrowStatementSyntax statement => GetThrowValues(statement.Expression),
			ThrowExpressionSyntax expression => GetThrowValues(expression.Expression),
			InvocationExpressionSyntax invocation => (GetInvocationName(invocation.Expression), null, null),
			ObjectCreationExpressionSyntax creation => GetTypeSyntaxValues(creation.Type),
			ImplicitObjectCreationExpressionSyntax creation => ("new", null, null),
			IdentifierNameSyntax identifier => (identifier.Identifier.ValueText, null, null),
			GenericNameSyntax identifier => (identifier.Identifier.ValueText, null, null),
			MemberAccessExpressionSyntax memberAccess => (memberAccess.Name.Identifier.ValueText, null, null),
			LiteralExpressionSyntax literal => (literal.Token.ValueText.Length > 0 ? literal.Token.ValueText : literal.Token.Text, null, null),
			_ => (node.ToString(), null, null)
		};

		return result;
	}

	private static (string SubjectName, string? AssociatedTypeName, string? AssociatedTypeNamespace) GetThrowValues(ExpressionSyntax? expression)
	{
		if (expression is null)
		{
			(string SubjectName, string? AssociatedTypeName, string? AssociatedTypeNamespace) emptyThrowResult = ("throw", null, null);

			return emptyThrowResult;
		}

		if (expression is ObjectCreationExpressionSyntax objectCreation)
		{
			var objectCreationResult = GetTypeSyntaxValues(objectCreation.Type);

			return objectCreationResult;
		}

		var expressionName = GetExpressionName(expression);
		(string SubjectName, string? AssociatedTypeName, string? AssociatedTypeNamespace) result = (expressionName, null, null);

		return result;
	}

	private static (string SubjectName, string? AssociatedTypeName, string? AssociatedTypeNamespace) GetTypeSyntaxValues(TypeSyntax typeSyntax)
	{
		var fullName = typeSyntax.ToString().Trim();
		var lastSeparator = fullName.LastIndexOf('.');
		var typeName = lastSeparator >= 0 ? fullName.Substring(lastSeparator + 1) : fullName;
		var namespaceName = lastSeparator > 0 ? fullName.Substring(0, lastSeparator) : string.Empty;
		var result = (typeName, typeName, string.IsNullOrEmpty(namespaceName) ? null : namespaceName);

		return result;
	}

	private static string GetInvocationName(ExpressionSyntax expression)
	{
		var result = expression switch
		{
			MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
			MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			_ => expression.ToString()
		};

		return result;
	}

	private static string GetExpressionName(ExpressionSyntax expression)
	{
		var result = expression switch
		{
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			GenericNameSyntax genericName => genericName.Identifier.ValueText,
			MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
			MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
			InvocationExpressionSyntax invocation => GetInvocationName(invocation.Expression),
			LiteralExpressionSyntax literal => literal.Token.ValueText.Length > 0 ? literal.Token.ValueText : literal.Token.Text,
			_ => expression.ToString()
		};

		return result;
	}
}
