namespace RonSijm.AnaalIJzer.Reporting;

internal sealed class ViolationRecord(string diagnosticId, string callerTypeName, string callerLayerName, string dependencyTypeName, string depLayerName, string violationReason, string? comment)
{
	public string DiagnosticId { get; } = diagnosticId;

	public string CallerTypeName { get; } = callerTypeName;

	public string CallerLayerName { get; } = callerLayerName;

	public string DependencyTypeName { get; } = dependencyTypeName;

	public string DepLayerName { get; } = depLayerName;

	public string ViolationReason { get; } = violationReason;

	public string? Comment { get; } = comment;
}
