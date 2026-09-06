using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Matchers.ProjectArchitecture;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct AssemblyReferenceViolationFinding(
	string sourceProjectPath,
	string sourceProjectName,
	string? sourceProjectGroup,
	string assemblyIdentity,
	string? hintPath,
	string violationReason,
	AssemblyReferencePolicy? matchedPolicy,
	ReferenceIdentityMatcher? matchedMatcher)
{
	public string SourceProjectPath { get; } = sourceProjectPath;

	public string SourceProjectName { get; } = sourceProjectName;

	public string? SourceProjectGroup { get; } = sourceProjectGroup;

	public string AssemblyIdentity { get; } = assemblyIdentity;

	public string? HintPath { get; } = hintPath;

	public string ViolationReason { get; } = violationReason;

	public AssemblyReferencePolicy? MatchedPolicy { get; } = matchedPolicy;

	public ReferenceIdentityMatcher? MatchedMatcher { get; } = matchedMatcher;

	public ArchitectureFinding ToArchitectureFinding()
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectPath, SourceProjectPath)
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectName, SourceProjectName)
			.Add(ArchitectureDiagnosticProperties.PropertySourceProjectGroup, SourceProjectGroup)
			.Add(ArchitectureDiagnosticProperties.PropertyAssemblyIdentity, AssemblyIdentity)
			.Add(ArchitectureDiagnosticProperties.PropertyAssemblyHintPath, HintPath)
			.Add(ArchitectureDiagnosticProperties.PropertyViolationReason, ViolationReason);
		var hintPathContext = string.IsNullOrWhiteSpace(HintPath) ? string.Empty : $" (HintPath: {HintPath})";
		var context = $"{SourceProjectName} -> {AssemblyIdentity}{hintPathContext}";
		var result = new ArchitectureFinding(
			ArchitectureFindingSeverity.Error,
			ArchitectureFindingCodes.AssemblyReferencePolicyViolation,
			ViolationReason,
			context,
			properties: properties);

		return result;
	}
}
