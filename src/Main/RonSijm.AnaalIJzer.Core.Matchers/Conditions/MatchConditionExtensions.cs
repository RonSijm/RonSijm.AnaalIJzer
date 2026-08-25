using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Symbols;

namespace RonSijm.AnaalIJzer.Conditions;

internal static class MatchConditionExtensions
{
	internal static bool Matches(this MatchCondition condition, MatchTarget target, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		var subject = target.GetSubject(typeName, namespaceName, symbol);
		var result = condition.Kind switch
		{
			MatchKind.EqualsFullName => string.Equals(typeName.ToFullName(namespaceName), condition.Value, StringComparison.Ordinal),
			MatchKind.Inherits => symbol is not null && symbol.InheritsFrom(condition.Value),
			MatchKind.Implements => symbol is not null && symbol.ImplementsInterface(condition.Value),
			MatchKind.HasAttribute => symbol is not null && symbol.HasAttribute(condition.Value),
			MatchKind.HasAccessModifier => symbol is not null && symbol.HasAccessModifier(condition.Value),
			MatchKind.HasTypeKind => symbol is not null && symbol.HasTypeKind(condition.Value),
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
