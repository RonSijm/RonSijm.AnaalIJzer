using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Graphing.Model;

public sealed class ArchitectureGraphEvidence(
	ImmutableArray<ArchitectureGraphTypeEvidence> types,
	ImmutableArray<ArchitectureGraphDependencyEvidence> dependencies)
{
	public ImmutableArray<ArchitectureGraphTypeEvidence> Types { get; } = types.IsDefault ? ImmutableArray<ArchitectureGraphTypeEvidence>.Empty : types;

	public ImmutableArray<ArchitectureGraphDependencyEvidence> Dependencies { get; } = dependencies.IsDefault ? ImmutableArray<ArchitectureGraphDependencyEvidence>.Empty : dependencies;

	public bool HasEvidence
	{
		get
		{
			var result = Types.Length > 0 || Dependencies.Length > 0;

			return result;
		}
	}

	public static ArchitectureGraphEvidence Empty { get; } = new(
		ImmutableArray<ArchitectureGraphTypeEvidence>.Empty,
		ImmutableArray<ArchitectureGraphDependencyEvidence>.Empty);
}

public sealed class ArchitectureGraphTypeEvidence(
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

public sealed class ArchitectureGraphDependencyEvidence(
	string callerLayerPath,
	string dependencyLayerPath,
	string callerTypeName,
	string dependencyTypeName,
	string site,
	string status,
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

	public string Status { get; } = status;

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
