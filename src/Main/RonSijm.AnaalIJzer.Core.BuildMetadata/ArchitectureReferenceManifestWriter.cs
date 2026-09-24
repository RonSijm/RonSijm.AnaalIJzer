namespace RonSijm.AnaalIJzer.Core.BuildMetadata;

internal static class ArchitectureReferenceManifestWriter
{
    internal static string Write(ArchitectureReferenceManifest manifest)
    {
        var lines = new List<string> { ArchitectureReferenceManifest.Header };
        foreach (var projectReference in manifest.ProjectReferences)
        {
            lines.Add("Project\t" + projectReference.SourceProjectPath + "\t" + projectReference.TargetProjectPath);
        }

        foreach (var packageReference in manifest.PackageReferences)
        {
            lines.Add("Package\t" + packageReference.SourceProjectPath + "\t" + packageReference.PackageId + "\t" + packageReference.PackageVersion + "\t" + packageReference.ReferenceKind);
        }

        foreach (var assemblyReference in manifest.AssemblyReferences)
        {
            lines.Add("Assembly\t" + assemblyReference.SourceProjectPath + "\t" + assemblyReference.AssemblyIdentity + "\t" + (assemblyReference.HintPath ?? string.Empty));
        }

        var result = string.Join("\n", lines);

        return result;
    }
}