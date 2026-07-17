namespace RonSijm.AnaalIJzer.Conditions;

internal enum MatchKind
{
	EndsWith,
	StartsWith,
	Contains,
	Equals,
	EqualsFullName,
	Inherits,
	Implements,
	HasAttribute,
	HasAccessModifier,
	HasTypeKind,
	Regex
}
