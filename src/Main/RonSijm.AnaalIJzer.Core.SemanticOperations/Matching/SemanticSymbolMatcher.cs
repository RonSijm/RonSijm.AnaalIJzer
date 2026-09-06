using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Matching;

/// <summary>Matches a resolved member or declaration using the shared semantic matcher vocabulary.</summary>
public readonly struct SemanticSymbolMatcher(
	ImmutableArray<MatchCondition> containingTypeConditions,
	ImmutableArray<MatchCondition> memberConditions,
	ImmutableHashSet<SemanticOperationMemberKind>? memberKinds = null)
{
	public ImmutableArray<MatchCondition> ContainingTypeConditions { get; } = containingTypeConditions;

	public ImmutableArray<MatchCondition> MemberConditions { get; } = memberConditions;

	public ImmutableHashSet<SemanticOperationMemberKind> MemberKinds { get; } = memberKinds ?? ImmutableHashSet<SemanticOperationMemberKind>.Empty;

	public bool Matches(ISymbol? symbol)
	{
		if (symbol is null)
		{
			return false;
		}

		var matchSymbol = GetMatchSymbol(symbol);
		if (!MatchesContainingType(matchSymbol.ContainingType))
		{
			return false;
		}

		if (!MatchesMemberKind(matchSymbol))
		{
			return false;
		}

		var result = MatchesMember(matchSymbol);

		return result;
	}

	private bool MatchesMemberKind(ISymbol symbol)
	{
		if (MemberKinds.Count == 0)
		{
			return true;
		}

		if (!TryGetMemberKind(symbol, out var memberKind))
		{
			return false;
		}

		var result = MemberKinds.Contains(memberKind);

		return result;
	}

	private bool MatchesContainingType(INamedTypeSymbol? containingType)
	{
		if (ContainingTypeConditions.IsDefaultOrEmpty)
		{
			return true;
		}

		if (containingType is null)
		{
			return false;
		}

		var namespaceName = GetNamespaceName(containingType);
		var context = new MatchContext(
			containingType.Name,
			namespaceName,
			containingType,
			containingType.Name,
			namespaceName,
			containingType);
		var result = MatchesAll(context, ContainingTypeConditions);

		return result;
	}

	private bool MatchesMember(ISymbol symbol)
	{
		if (MemberConditions.IsDefaultOrEmpty)
		{
			return true;
		}

		var associatedType = GetAssociatedType(symbol);
		var context = new MatchContext(
			GetMemberName(symbol),
			GetNamespaceName(symbol),
			symbol,
			associatedType?.Name,
			associatedType is null ? null : GetNamespaceName(associatedType),
			associatedType);
		var result = MatchesAll(context, MemberConditions);

		return result;
	}

	private static bool MatchesAll(MatchContext context, ImmutableArray<MatchCondition> conditions)
	{
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

	private static ISymbol GetMatchSymbol(ISymbol symbol)
	{
		var result = symbol is IMethodSymbol { AssociatedSymbol: not null } method
			? method.AssociatedSymbol
			: symbol;

		return result;
	}

	private static string GetMemberName(ISymbol symbol)
	{
		var result = symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } constructor
			? constructor.ContainingType.Name
			: symbol.Name;

		return result;
	}

	private static string GetNamespaceName(ISymbol symbol)
	{
		var result = symbol.ContainingNamespace.IsGlobalNamespace
			? string.Empty
			: symbol.ContainingNamespace.ToDisplayString();

		return result;
	}

	private static ITypeSymbol? GetAssociatedType(ISymbol symbol)
	{
		var result = symbol switch
		{
			INamedTypeSymbol type => type,
			IPropertySymbol property => property.Type,
			IFieldSymbol field => field.Type,
			IEventSymbol @event => @event.Type,
			IParameterSymbol parameter => parameter.Type,
			IMethodSymbol method => method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor
				? method.ContainingType
				: method.ReturnType,
			_ => null
		};

		return result;
	}

	private static bool TryGetMemberKind(ISymbol symbol, out SemanticOperationMemberKind memberKind)
	{
		switch (symbol)
		{
			case IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor }:
				memberKind = SemanticOperationMemberKind.Constructor;
				return true;
			case IMethodSymbol:
				memberKind = SemanticOperationMemberKind.Method;
				return true;
			case IPropertySymbol:
				memberKind = SemanticOperationMemberKind.Property;
				return true;
			case IFieldSymbol:
				memberKind = SemanticOperationMemberKind.Field;
				return true;
			case IEventSymbol:
				memberKind = SemanticOperationMemberKind.Event;
				return true;
			default:
				memberKind = default;
				return false;
		}
	}
}
