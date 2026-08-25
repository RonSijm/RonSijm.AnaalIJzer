using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Findings;

namespace RonSijm.AnaalIJzer.ProjectArchitecture;

public readonly struct ProjectReferenceViolationFinding(
	string sourceProjectPath,
	string sourceProjectName,
	string? sourceProjectGroup,
	string targetProjectPath,
	string targetProjectName,
	string? targetProjectGroup,
	string violationReason,
	ProjectReferenceRule? matchedRule)
{
	public string SourceProjectPath { get; } = sourceProjectPath;
	public string SourceProjectName { get; } = sourceProjectName;
	public string? SourceProjectGroup { get; } = sourceProjectGroup;
	public string TargetProjectPath { get; } = targetProjectPath;
	public string TargetProjectName { get; } = targetProjectName;
	public string? TargetProjectGroup { get; } = targetProjectGroup;
	public string ViolationReason { get; } = violationReason;
	public ProjectReferenceRule? MatchedRule { get; } = matchedRule;

	public ArchitectureFinding ToArchitectureFinding()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectPath, SourceProjectPath)
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectName, SourceProjectName)
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectGroup, SourceProjectGroup)
			.Add(ArchitectureDiagnosticProperties.PropertyTargetProjectPath, TargetProjectPath)
			.Add(ArchitectureDiagnosticProperties.PropertyTargetProjectName, TargetProjectName)
			.Add(ArchitectureDiagnosticProperties.PropertyTargetProjectGroup, TargetProjectGroup)
			.Add(ArchitectureDiagnosticProperties.PropertyViolationReason, ViolationReason);
		var context = $"{SourceProjectName} -> {TargetProjectName}";
		var result = new ArchitectureFinding(
			ArchitectureFindingSeverity.Error,
			ArchitecturalDiagnosticIds.ProjectReferenceViolation,
			ViolationReason,
			context,
			properties: properties);

		return result;
	}
}
