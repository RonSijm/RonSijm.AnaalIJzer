namespace RonSijm.AnaalIJzer.Matching;

/// <summary>Describes why a dependency type failed an Allowed or Forbidden type policy.</summary>
internal readonly struct TypePolicyViolation
{
	public TypePolicyViolation(string reason, string dependencyLayerName, string? comment, MatcherRule? rule, string? matchedSuffix)
	{
		Reason = reason;
		DependencyLayerName = dependencyLayerName;
		Comment = comment;
		Rule = rule;
		MatchedSuffix = matchedSuffix;
	}

	public string Reason { get; }
	public string DependencyLayerName { get; }
	public string? Comment { get; }
	public MatcherRule? Rule { get; }
	public string? MatchedSuffix { get; }
}
