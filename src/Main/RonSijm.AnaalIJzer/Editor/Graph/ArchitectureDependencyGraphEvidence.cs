using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Indicators;

namespace RonSijm.AnaalIJzer.Graph;

public sealed class ArchitectureDependencyGraphEvidence(
	ImmutableArray<ArchitectureDependencyGraphTypeEvidence> types,
	ImmutableArray<ArchitectureDependencyGraphDependencyEvidence> dependencies)
{
	public ImmutableArray<ArchitectureDependencyGraphTypeEvidence> Types { get; } = types.IsDefault ? ImmutableArray<ArchitectureDependencyGraphTypeEvidence>.Empty : types;

	public ImmutableArray<ArchitectureDependencyGraphDependencyEvidence> Dependencies { get; } = dependencies.IsDefault ? ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Empty : dependencies;

	public bool HasEvidence
	{
		get
		{
			var result = Types.Length > 0 || Dependencies.Length > 0;

			return result;
		}
	}

	public static ArchitectureDependencyGraphEvidence Empty { get; } = new(
		ImmutableArray<ArchitectureDependencyGraphTypeEvidence>.Empty,
		ImmutableArray<ArchitectureDependencyGraphDependencyEvidence>.Empty);
}

public sealed class ArchitectureDependencyGraphTypeEvidence(
	string layerPath,
	string typeName,
	string fullTypeName,
	string filePath,
	int lineNumber)
{
	public string LayerPath { get; } = layerPath;

	public string TypeName { get; } = typeName;

	public string FullTypeName { get; } = fullTypeName;

	public string FilePath { get; } = filePath;

	public int LineNumber { get; } = lineNumber;
}

public sealed class ArchitectureDependencyGraphDependencyEvidence(
	string callerLayerPath,
	string dependencyLayerPath,
	string callerTypeName,
	string dependencyTypeName,
	string site,
	ArchitectureDependencySiteStatus status,
	string? diagnosticId,
	string reason,
	string filePath,
	int lineNumber)
{
	public string CallerLayerPath { get; } = callerLayerPath;

	public string DependencyLayerPath { get; } = dependencyLayerPath;

	public string CallerTypeName { get; } = callerTypeName;

	public string DependencyTypeName { get; } = dependencyTypeName;

	public string Site { get; } = site;

	public ArchitectureDependencySiteStatus Status { get; } = status;

	public string? DiagnosticId { get; } = diagnosticId;

	public string Reason { get; } = reason;

	public string FilePath { get; } = filePath;

	public int LineNumber { get; } = lineNumber;

	public bool IsViolation
	{
		get
		{
			var result = DiagnosticId is not null;

			return result;
		}
	}
}
