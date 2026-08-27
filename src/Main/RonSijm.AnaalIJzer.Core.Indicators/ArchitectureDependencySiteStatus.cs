namespace RonSijm.AnaalIJzer.Core.Indicators;

public enum ArchitectureDependencySiteStatus
{
	Allowed,
	MissingAllowedDependency,
	SiteFiltered,
	Blocked,
	WrongDirection,
	SameLayer,
	Unrecognized,
	Unclassified,
	TypePolicyViolation
}
