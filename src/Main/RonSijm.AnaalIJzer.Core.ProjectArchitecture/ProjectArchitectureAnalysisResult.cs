using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Findings;

namespace RonSijm.AnaalIJzer.ProjectArchitecture;

public readonly struct ProjectArchitectureAnalysisResult(
	ImmutableArray<ProjectReferenceViolationFinding> projectReferenceViolations,
	ImmutableArray<PackageReferenceViolationFinding> packageReferenceViolations)
{
	public ImmutableArray<ProjectReferenceViolationFinding> ProjectReferenceViolations { get; } = projectReferenceViolations;
	public ImmutableArray<PackageReferenceViolationFinding> PackageReferenceViolations { get; } = packageReferenceViolations;

	public ImmutableArray<ArchitectureFinding> Findings
	{
		get
		{
			var builder = ImmutableArray.CreateBuilder<ArchitectureFinding>(ProjectReferenceViolations.Length + PackageReferenceViolations.Length);
			builder.AddRange(ProjectReferenceViolations.Select(violation => violation.ToArchitectureFinding()));
			builder.AddRange(PackageReferenceViolations.Select(violation => violation.ToArchitectureFinding()));
			var result = builder.ToImmutable();

			return result;
		}
	}
}
