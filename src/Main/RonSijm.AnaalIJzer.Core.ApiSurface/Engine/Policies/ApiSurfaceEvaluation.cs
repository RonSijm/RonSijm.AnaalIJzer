namespace RonSijm.AnaalIJzer.Core.ApiSurface.Engine.Policies;

public readonly struct ApiSurfaceEvaluation(ApiSurfacePolicy policy, ApiSurfaceLayerRule? rule, string reason)
{
	public ApiSurfacePolicy Policy { get; } = policy;
	public ApiSurfaceLayerRule? Rule { get; } = rule;
	public string Reason { get; } = reason;
}
