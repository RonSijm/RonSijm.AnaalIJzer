using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers.Symbols;

namespace RonSijm.AnaalIJzer.Core.Matchers.Conditions;

internal static class MatchConditionExtensions
{
	internal static bool Matches(this MatchCondition condition, MatchTarget target, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		var context = MatchContext.Create(target, typeName, namespaceName, symbol);
		var result = condition.Matches(context);

		return result;
	}

	internal static bool Matches(this MatchCondition condition, MatchContext context)
	{
		var subject = context.GetName(condition.Operand);
		var result = condition.Kind switch
		{
			MatchKind.EqualsFullName => string.Equals(context.GetName(condition.Operand).ToFullName(context.GetNamespace(condition.Operand)), condition.Value, StringComparison.Ordinal),
			MatchKind.Inherits => context.GetTypeSymbol(condition.Operand) is { } inheritedType && inheritedType.InheritsFrom(condition.Value),
			MatchKind.Implements => context.GetTypeSymbol(condition.Operand) is { } implementedType && implementedType.ImplementsInterface(condition.Value),
			MatchKind.HasAttribute => context.GetSymbol(condition.Operand) is { } attributedSymbol && attributedSymbol.HasAttribute(condition.Value),
			MatchKind.HasAccessModifier => context.GetSymbol(condition.Operand) is { } modifiedSymbol && modifiedSymbol.HasAccessModifier(condition.Value),
			MatchKind.HasTypeKind => context.GetTypeSymbol(condition.Operand) is { } kindSymbol && kindSymbol.HasTypeKind(condition.Value),
			_ => condition.MatchesString(subject, StringComparison.Ordinal, RegexOptions.CultureInvariant)
		};

		return result;
	}

	internal static bool MatchesString(this MatchCondition condition, string subject, StringComparison comparison, RegexOptions regexOptions)
	{
		var result = condition.Kind switch
		{
			MatchKind.EndsWith => subject.EndsWith(condition.Value, comparison),
			MatchKind.StartsWith => subject.StartsWith(condition.Value, comparison),
			MatchKind.Contains => subject.IndexOf(condition.Value, comparison) >= 0,
			MatchKind.Equals => string.Equals(subject, condition.Value, comparison),
			MatchKind.Regex => subject.MatchesRegexPattern(condition.Value, regexOptions),
			_ => false
		};

		return result;
	}
}
