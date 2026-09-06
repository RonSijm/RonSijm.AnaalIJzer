using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.ProjectArchitecture;
using RonSijm.AnaalIJzer.Workspace.Analysis;

namespace RonSijm.AnaalIJzer.Application;

/// <summary>
///     Evaluates project-architecture evidence which is available only to a workspace host.
/// </summary>
internal static class ProjectArchitectureInspectionService
{
	internal static ImmutableArray<ArchitectureFinding> GetAssemblyReferencePolicyFindings(IEnumerable<ProjectAnalysisResult> projects)
	{
		var findings = ImmutableArray.CreateBuilder<ArchitectureFinding>();
		foreach (var project in projects)
		{
			if (project.Config.ProjectArchitecture.AssemblyReferencePolicies.IsDefaultOrEmpty)
			{
				continue;
			}

			var analysis = ProjectArchitectureAnalysisService.Analyze(project.Config.ProjectArchitecture, project.ReferenceManifest);
			findings.AddRange(analysis.AssemblyReferenceViolations.Select(violation => violation.ToArchitectureFinding()));
		}

		var result = findings
			.OrderBy(finding => finding.Properties.GetValueOrDefault(ArchitectureDiagnosticProperties.PropertySourceProjectName), StringComparer.Ordinal)
			.ThenBy(finding => finding.Properties.GetValueOrDefault(ArchitectureDiagnosticProperties.PropertyAssemblyIdentity), StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}
}
