namespace RonSijm.AnaalIJzer.Conditions;

public enum MatchKind
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
