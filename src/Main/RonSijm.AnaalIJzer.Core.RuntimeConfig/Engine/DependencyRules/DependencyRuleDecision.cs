using RonSijm.AnaalIJzer.Core.DependencyRules;
using RonSijm.AnaalIJzer.Core.Indicators;
using RonSijm.AnaalIJzer.Core.LayerModel;

namespace RonSijm.AnaalIJzer.Core.RuntimeConfig.Engine.DependencyRules;

public readonly struct DependencyRuleDecision(
	string dependencyLayerName,
	ArchitectureDependencySiteStatus status,
	string? diagnosticId,
	string reason,
	TypePolicyViolation? typePolicyViolation,
	DependencyEdgeEvaluation? edgeEvaluation,
	bool isForbiddenLayer)
{
	public string DependencyLayerName { get; } = dependencyLayerName;
	public ArchitectureDependencySiteStatus Status { get; } = status;
	public string? DiagnosticId { get; } = diagnosticId;
	public string Reason { get; } = reason;
	public TypePolicyViolation? TypePolicyViolation { get; } = typePolicyViolation;
	public DependencyEdgeEvaluation? EdgeEvaluation { get; } = edgeEvaluation;
	public bool IsForbiddenLayer { get; } = isForbiddenLayer;

	public bool IsAllowed
	{
		get
		{
			var result = Status == ArchitectureDependencySiteStatus.Allowed;

			return result;
		}
	}
}
