namespace RonSijm.IJzer.Core.Indicators;

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