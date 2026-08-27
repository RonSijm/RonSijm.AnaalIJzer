namespace RonSijm.AnaalIJzer.Core.EntryPoints;

public readonly struct BoundaryEntryPointEvaluation(
	bool isAllowed,
	string boundaryLayerName,
	string reason,
	string? matchedEntryPoint,
	string? xmlPath,
	int xmlLineNumber,
	int xmlLinePosition)
{
	public static BoundaryEntryPointEvaluation Allowed { get; } = new(true, string.Empty, string.Empty, null, null, 0, 0);

	public bool IsAllowed { get; } = isAllowed;

	public string BoundaryLayerName { get; } = boundaryLayerName;

	public string Reason { get; } = reason;

	public string? MatchedEntryPoint { get; } = matchedEntryPoint;

	public string? XmlPath { get; } = xmlPath;

	public int XmlLineNumber { get; } = xmlLineNumber;

	public int XmlLinePosition { get; } = xmlLinePosition;

	public static BoundaryEntryPointEvaluation Denied(BoundaryEntryPointPolicy policy, string reason, string? matchedEntryPoint, BoundaryEntryPointRule? rule = null)
	{
		var result = new BoundaryEntryPointEvaluation(
			false,
			policy.OwnerLayerPath,
			reason,
			matchedEntryPoint,
			rule?.XmlPath ?? policy.XmlPath,
			rule?.XmlLineNumber ?? policy.XmlLineNumber,
			rule?.XmlLinePosition ?? policy.XmlLinePosition);

		return result;
	}
}
