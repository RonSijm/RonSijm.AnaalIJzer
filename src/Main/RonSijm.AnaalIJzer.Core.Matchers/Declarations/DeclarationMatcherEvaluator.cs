using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.Matchers.Observations;

namespace RonSijm.AnaalIJzer.Core.Matchers.Declarations;

internal static class DeclarationMatcherEvaluator
{
	internal static bool MatchesAll(INamedTypeSymbol? typeSymbol, ImmutableArray<DeclarationMatcher> matchers)
	{
		if (matchers.IsDefaultOrEmpty)
		{
			return true;
		}

		if (typeSymbol is null)
		{
			return false;
		}

		foreach (var matcher in matchers)
		{
			if (!MatchesAny(typeSymbol, matcher))
			{
				return false;
			}
		}

		return true;
	}

	private static bool MatchesAny(INamedTypeSymbol typeSymbol, DeclarationMatcher matcher)
	{
		foreach (var declaration in GetCandidateDeclarations(typeSymbol, matcher.Target))
		{
			if (MatchesAllConditions(declaration, matcher.Conditions)
			    && CodeObservationMatcherEvaluator.MatchesAll(declaration, matcher.RequiredObservations))
			{
				return true;
			}
		}

		var result = false;

		return result;
	}

	private static bool MatchesAllConditions(ISymbol declarationSymbol, ImmutableArray<MatchCondition> conditions)
	{
		var associatedType = GetAssociatedType(declarationSymbol);
		var associatedTypeNamespace = associatedType is null || associatedType.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: associatedType.ContainingNamespace.ToDisplayString();
		var context = new MatchContext(
			GetDeclarationName(declarationSymbol),
			string.Empty,
			declarationSymbol,
			associatedType?.Name,
			associatedTypeNamespace,
			associatedType);

		foreach (var condition in conditions)
		{
			if (!condition.Matches(context))
			{
				return false;
			}
		}

		return true;
	}

	private static string GetDeclarationName(ISymbol declarationSymbol)
	{
		var result = declarationSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } constructor
			? constructor.ContainingType.Name
			: declarationSymbol.Name;

		return result;
	}

	private static IEnumerable<ISymbol> GetCandidateDeclarations(INamedTypeSymbol typeSymbol, DeclarationMatchTarget target)
	{
		var members = typeSymbol.GetMembers().Where(member => !member.IsImplicitlyDeclared);
		var result = target switch
		{
			DeclarationMatchTarget.Type => GetTypeCandidate(typeSymbol),
			DeclarationMatchTarget.NestedType => members.OfType<INamedTypeSymbol>().Where(type => type.ContainingType is not null),
			DeclarationMatchTarget.Constructor => members.OfType<IMethodSymbol>().Where(IsConstructor),
			DeclarationMatchTarget.Method => members.OfType<IMethodSymbol>().Where(IsMethod),
			DeclarationMatchTarget.Property => members.OfType<IPropertySymbol>(),
			DeclarationMatchTarget.Field => members.OfType<IFieldSymbol>(),
			DeclarationMatchTarget.Event => members.OfType<IEventSymbol>(),
			DeclarationMatchTarget.Operator => members.OfType<IMethodSymbol>().Where(IsOperator),
			DeclarationMatchTarget.Conversion => members.OfType<IMethodSymbol>().Where(IsConversion),
			_ => []
		};

		return result;
	}

	private static IEnumerable<ISymbol> GetTypeCandidate(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.IsImplicitlyDeclared)
		{
			return [];
		}

		var result = new[] { typeSymbol };

		return result;
	}

	private static bool IsConstructor(IMethodSymbol method)
	{
		var result = method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor;

		return result;
	}

	private static bool IsMethod(IMethodSymbol method)
	{
		var result = method.MethodKind is MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation;

		return result;
	}

	private static bool IsOperator(IMethodSymbol method)
	{
		var result = method.MethodKind == MethodKind.UserDefinedOperator;

		return result;
	}

	private static bool IsConversion(IMethodSymbol method)
	{
		var result = method.MethodKind == MethodKind.Conversion;

		return result;
	}

	private static ITypeSymbol? GetAssociatedType(ISymbol declarationSymbol)
	{
		var result = declarationSymbol switch
		{
			INamedTypeSymbol type => type,
			IPropertySymbol property => property.Type,
			IFieldSymbol field => field.Type,
			IEventSymbol @event => @event.Type,
			IMethodSymbol method => GetMethodAssociatedType(method),
			_ => null
		};

		return result;
	}

	private static ITypeSymbol? GetMethodAssociatedType(IMethodSymbol method)
	{
		var result = method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor
			? method.ContainingType
			: method.ReturnType;

		return result;
	}
}
