namespace RonSijm.AnaalIJzer.Engine.DependencyRules;

public enum DependencyDenialKind
{
	None,
	MissingEdge,
	SiteFilter,
	BlockedEdge
}

public readonly struct DependencyEdgeEvaluation(bool isAllowed, string denialReason, DependencyDenialKind denialKind, string scopePath, string fromPath, string toPath)
{
	public static DependencyEdgeEvaluation Allowed { get; } = new(true, string.Empty, DependencyDenialKind.None, string.Empty, string.Empty, string.Empty);

	public bool IsAllowed { get; } = isAllowed;

	public string DenialReason { get; } = denialReason;

	public DependencyDenialKind DenialKind { get; } = denialKind;

	public string ScopePath { get; } = scopePath;

	public string FromPath { get; } = fromPath;

	public string ToPath { get; } = toPath;

	public bool IsDeniedBySiteFilter
	{
		get { return DenialKind == DependencyDenialKind.SiteFilter; }
	}

	public bool IsDeniedByBlockedEdge
	{
		get { return DenialKind == DependencyDenialKind.BlockedEdge; }
	}

	public static DependencyEdgeEvaluation Denied(string reason, DependencyDenialKind denialKind, string scopePath, string fromPath, string toPath)
	{
		var result = new DependencyEdgeEvaluation(false, reason, denialKind, scopePath, fromPath, toPath);

		return result;
	}
}
