using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.BuildMetadata;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct PackageReferenceViolationFinding(
	string sourceProjectPath,
	string sourceProjectName,
	string? sourceProjectGroup,
	string packageId,
	string packageVersion,
	PackageReferenceKind referenceKind,
	string violationReason,
	PackagePolicy? matchedPolicy,
	PackageMatcher? matchedMatcher)
{
	public string SourceProjectPath { get; } = sourceProjectPath;
	public string SourceProjectName { get; } = sourceProjectName;
	public string? SourceProjectGroup { get; } = sourceProjectGroup;
	public string PackageId { get; } = packageId;
	public string PackageVersion { get; } = packageVersion;
	public PackageReferenceKind ReferenceKind { get; } = referenceKind;
	public string ViolationReason { get; } = violationReason;
	public PackagePolicy? MatchedPolicy { get; } = matchedPolicy;
	public PackageMatcher? MatchedMatcher { get; } = matchedMatcher;

	public ArchitectureFinding ToArchitectureFinding()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectPath, SourceProjectPath)
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectName, SourceProjectName)
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectGroup, SourceProjectGroup)
			.Add(ArchitectureDiagnosticProperties.PropertyPackageId, PackageId)
			.Add(ArchitectureDiagnosticProperties.PropertyPackageVersion, PackageVersion)
			.Add(ArchitectureDiagnosticProperties.PropertyPackageReferenceKind, ReferenceKind.ToString())
			.Add(ArchitectureDiagnosticProperties.PropertyViolationReason, ViolationReason);
		var context = $"{SourceProjectName} -> {PackageId}";
		var result = new ArchitectureFinding(
			ArchitectureFindingSeverity.Error,
			ArchitecturalDiagnosticIds.PackageReferenceViolation,
			ViolationReason,
			context,
			properties: properties);

		return result;
	}
}
