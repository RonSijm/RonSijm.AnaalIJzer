namespace RonSijm.AnaalIJzer.Engine.ApiSurface;

public readonly struct ApiSurfaceEvaluation(ApiSurfacePolicy policy, ApiSurfaceLayerRule? rule, string reason)
{
	public ApiSurfacePolicy Policy { get; } = policy;
	public ApiSurfaceLayerRule? Rule { get; } = rule;
	public string Reason { get; } = reason;
}
