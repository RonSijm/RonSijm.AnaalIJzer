namespace RonSijm.AnaalIJzer.Core.BuildMetadata;

public readonly struct ArchitecturePackageReference(
    string sourceProjectPath,
    string packageId,
    string packageVersion,
    PackageReferenceKind referenceKind)
{
    public string SourceProjectPath { get; } = sourceProjectPath;

    public string PackageId { get; } = packageId;

    public string PackageVersion { get; } = packageVersion;

    public PackageReferenceKind ReferenceKind { get; } = referenceKind;
}

public enum PackageReferenceKind
{
    Direct,
    Transitive
}
