using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Core.ProjectArchitecture;

public readonly struct ProjectArchitectureAnalysisResult(
    ImmutableArray<ProjectReferenceViolationFinding> projectReferenceViolations,
    ImmutableArray<PackageReferenceViolationFinding> packageReferenceViolations,
    ImmutableArray<AssemblyReferenceViolationFinding> assemblyReferenceViolations)
{
    public ImmutableArray<ProjectReferenceViolationFinding> ProjectReferenceViolations { get; } = projectReferenceViolations;
    public ImmutableArray<PackageReferenceViolationFinding> PackageReferenceViolations { get; } = packageReferenceViolations;
    public ImmutableArray<AssemblyReferenceViolationFinding> AssemblyReferenceViolations { get; } = assemblyReferenceViolations;

    public ImmutableArray<ArchitectureFinding> Findings
    {
        get
        {
            var builder = ImmutableArray.CreateBuilder<ArchitectureFinding>(ProjectReferenceViolations.Length + PackageReferenceViolations.Length + AssemblyReferenceViolations.Length);
            builder.AddRange(ProjectReferenceViolations.Select(violation => violation.ToArchitectureFinding()));
            builder.AddRange(PackageReferenceViolations.Select(violation => violation.ToArchitectureFinding()));
            builder.AddRange(AssemblyReferenceViolations.Select(violation => violation.ToArchitectureFinding()));
            var result = builder.ToImmutable();

            return result;
        }
    }
}