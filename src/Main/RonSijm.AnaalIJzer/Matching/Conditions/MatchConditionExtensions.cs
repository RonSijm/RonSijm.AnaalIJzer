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
			MatchKind.EndsWith => subject.EndsWith(condition.Value, StringComparison.Ordinal),
			MatchKind.StartsWith => subject.StartsWith(condition.Value, StringComparison.Ordinal),
			MatchKind.Contains => subject.Contains(condition.Value),
			MatchKind.Equals => string.Equals(subject, condition.Value, StringComparison.Ordinal),
			MatchKind.EqualsFullName => string.Equals(typeName.ToFullName(namespaceName), condition.Value, StringComparison.Ordinal),
			MatchKind.Inherits => symbol is not null && symbol.InheritsFrom(condition.Value),
			MatchKind.Implements => symbol is not null && symbol.ImplementsInterface(condition.Value),
			MatchKind.HasAttribute => symbol is not null && symbol.HasAttribute(condition.Value),
			MatchKind.HasAccessModifier => symbol is not null && symbol.HasAccessModifier(condition.Value),
			MatchKind.HasTypeKind => symbol is not null && symbol.HasTypeKind(condition.Value),
			MatchKind.Regex => subject.MatchesRegexPattern(condition.Value),
			_ => false
		};

		return result;
	}
}
