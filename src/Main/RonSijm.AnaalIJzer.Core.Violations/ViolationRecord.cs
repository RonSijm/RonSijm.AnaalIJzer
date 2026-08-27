namespace RonSijm.AnaalIJzer.Core.Violations;

public sealed class ViolationRecord(
	string diagnosticId,
	string callerTypeName,
	string callerLayerName,
	string dependencyTypeName,
	string depLayerName,
	string violationReason,
	string? comment,
	string? declarationTarget = null,
	string? declaredAccessibility = null,
	string? apiMemberName = null,
	string? exposurePath = null,
	int? exposureDepth = null,
	string? nestedMemberName = null,
	string? sourceProjectPath = null,
	string? sourceProjectName = null,
	string? sourceProjectGroup = null,
	string? targetProjectPath = null,
	string? targetProjectName = null,
	string? targetProjectGroup = null,
	string? packageId = null,
	string? packageVersion = null,
	string? packageReferenceKind = null,
	string? sourceFilePath = null,
	string? normalizedSourcePath = null,
	string? sourceAssemblyName = null,
	string? boundaryLayerName = null,
	string? matchedEntryPoint = null,
	string? cycleLayers = null,
	int? cycleLength = null,
	string? observedSites = null,
	string? cycleScope = null)
{
	public string DiagnosticId { get; } = diagnosticId;

	public string CallerTypeName { get; } = callerTypeName;

	public string CallerLayerName { get; } = callerLayerName;

	public string DependencyTypeName { get; } = dependencyTypeName;

	public string DepLayerName { get; } = depLayerName;

	public string ViolationReason { get; } = violationReason;

	public string? Comment { get; } = comment;

	public string? DeclarationTarget { get; } = declarationTarget;

	public string? DeclaredAccessibility { get; } = declaredAccessibility;

	public string? ApiMemberName { get; } = apiMemberName;

	public string? ExposurePath { get; } = exposurePath;

	public int? ExposureDepth { get; } = exposureDepth;

	public string? NestedMemberName { get; } = nestedMemberName;

	public string? SourceProjectPath { get; } = sourceProjectPath;

	public string? SourceProjectName { get; } = sourceProjectName;

	public string? SourceProjectGroup { get; } = sourceProjectGroup;

	public string? TargetProjectPath { get; } = targetProjectPath;

	public string? TargetProjectName { get; } = targetProjectName;

	public string? TargetProjectGroup { get; } = targetProjectGroup;

	public string? PackageId { get; } = packageId;

	public string? PackageVersion { get; } = packageVersion;

	public string? PackageReferenceKind { get; } = packageReferenceKind;

	public string? SourceFilePath { get; } = sourceFilePath;

	public string? NormalizedSourcePath { get; } = normalizedSourcePath;

	public string? SourceAssemblyName { get; } = sourceAssemblyName;

	public string? BoundaryLayerName { get; } = boundaryLayerName;

	public string? MatchedEntryPoint { get; } = matchedEntryPoint;

	public string? CycleLayers { get; } = cycleLayers;

	public int? CycleLength { get; } = cycleLength;

	public string? ObservedSites { get; } = observedSites;

	public string? CycleScope { get; } = cycleScope;
}
