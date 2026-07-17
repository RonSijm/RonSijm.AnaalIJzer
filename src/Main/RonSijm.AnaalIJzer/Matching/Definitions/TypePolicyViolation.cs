using RonSijm.AnaalIJzer.Conditions;

namespace RonSijm.AnaalIJzer.Definitions;

/// <summary>Describes why a dependency type failed an Allowed or Forbidden type policy.</summary>
internal readonly struct TypePolicyViolation(
    string reason,
    string dependencyLayerName,
    string? comment,
    MatcherRule? rule,
    string? matchedSuffix)
{
    public string Reason { get; } = reason;
    public string DependencyLayerName { get; } = dependencyLayerName;
    public string? Comment { get; } = comment;
    public MatcherRule? Rule { get; } = rule;
    public string? MatchedSuffix { get; } = matchedSuffix;
}
