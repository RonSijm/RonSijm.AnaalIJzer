using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.BuildMetadata;

public readonly struct ArchitectureReferenceManifest(
    ImmutableArray<ProjectReferenceManifestRecord> projectReferences,
    ImmutableArray<ArchitecturePackageReference> packageReferences,
    ImmutableArray<ArchitectureAssemblyReference> assemblyReferences = default)
{
    public const string Header = "AnaalIJzerReferenceManifest/1";
    public const string FileName = "AnaalIJzerReferenceManifest.txt";

    public static readonly ArchitectureReferenceManifest Empty = new(
        ImmutableArray<ProjectReferenceManifestRecord>.Empty,
        ImmutableArray<ArchitecturePackageReference>.Empty,
        ImmutableArray<ArchitectureAssemblyReference>.Empty);

    public ImmutableArray<ProjectReferenceManifestRecord> ProjectReferences { get; } = projectReferences;

    public ImmutableArray<ArchitecturePackageReference> PackageReferences { get; } = packageReferences;

    public ImmutableArray<ArchitectureAssemblyReference> AssemblyReferences { get; } = assemblyReferences.IsDefault
        ? ImmutableArray<ArchitectureAssemblyReference>.Empty
        : assemblyReferences;
}

public readonly struct ProjectReferenceManifestRecord(string sourceProjectPath, string targetProjectPath)
{
    public string SourceProjectPath { get; } = sourceProjectPath;

    public string TargetProjectPath { get; } = targetProjectPath;
}

public readonly struct ArchitectureAssemblyReference(
    string sourceProjectPath,
    string assemblyIdentity,
    string? hintPath)
{
    public string SourceProjectPath { get; } = sourceProjectPath;

    public string AssemblyIdentity { get; } = assemblyIdentity;

    public string? HintPath { get; } = hintPath;
}
