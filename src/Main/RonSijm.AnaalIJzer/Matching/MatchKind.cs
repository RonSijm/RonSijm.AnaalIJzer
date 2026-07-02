namespace RonSijm.AnaalIJzer.Matching;

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
	Regex
}
