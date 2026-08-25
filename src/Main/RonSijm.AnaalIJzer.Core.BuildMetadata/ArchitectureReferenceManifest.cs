using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.BuildMetadata;

public readonly struct ArchitectureReferenceManifest(
    ImmutableArray<ProjectReferenceManifestRecord> projectReferences,
    ImmutableArray<ArchitecturePackageReference> packageReferences)
{
    public const string Header = "AnaalIJzerReferenceManifest/1";
    public const string FileName = "AnaalIJzerReferenceManifest.txt";

    public static readonly ArchitectureReferenceManifest Empty = new(
        ImmutableArray<ProjectReferenceManifestRecord>.Empty,
        ImmutableArray<ArchitecturePackageReference>.Empty);

    public ImmutableArray<ProjectReferenceManifestRecord> ProjectReferences { get; } = projectReferences;

    public ImmutableArray<ArchitecturePackageReference> PackageReferences { get; } = packageReferences;
}

public readonly struct ProjectReferenceManifestRecord(string sourceProjectPath, string targetProjectPath)
{
    public string SourceProjectPath { get; } = sourceProjectPath;

    public string TargetProjectPath { get; } = targetProjectPath;
}
