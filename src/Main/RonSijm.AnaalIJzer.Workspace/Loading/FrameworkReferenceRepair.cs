using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal static class FrameworkReferenceRepair
{
    public static Compilation RepairIfNeeded(Compilation compilation, string? targetFramework)
    {
        if (compilation.GetTypeByMetadataName("System.Object") is not null
            || !TryGetReferenceDirectory(targetFramework, out var referenceDirectory))
        {
            return compilation;
        }

        var existingReferencePaths = compilation.References
            .Select(reference => reference.Display)
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(path => Path.GetFullPath(path!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var frameworkReferences = Directory
            .EnumerateFiles(referenceDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Where(path => !existingReferencePaths.Contains(Path.GetFullPath(path)))
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
        var result = frameworkReferences.Length == 0 ? compilation : compilation.AddReferences(frameworkReferences);

        return result;
    }

    private static bool TryGetReferenceDirectory(string? targetFramework, out string referenceDirectory)
    {
        referenceDirectory = string.Empty;
        var targetFrameworkMoniker = GetBaseTargetFramework(targetFramework);
        if (!TryParseNetCoreVersion(targetFrameworkMoniker, out var targetVersion))
        {
            return false;
        }

        var dotNetRoot = GetDotNetRoot();
        if (dotNetRoot is null)
        {
            return false;
        }

        var packRoot = Path.Combine(dotNetRoot, "packs", "Microsoft.NETCore.App.Ref");
        if (!Directory.Exists(packRoot))
        {
            return false;
        }

        var matchingPackDirectory = Directory
            .EnumerateDirectories(packRoot)
            .Select(path => new { Path = path, Version = Path.GetFileName(path) })
            .Where(candidate => Version.TryParse(candidate.Version, out var version)
                && version.Major == targetVersion.Major
                && version.Minor == targetVersion.Minor)
            .OrderByDescending(candidate => Version.Parse(candidate.Version))
            .Select(candidate => candidate.Path)
            .FirstOrDefault(path => Directory.Exists(Path.Combine(path, "ref", targetFrameworkMoniker)));
        if (matchingPackDirectory is null)
        {
            return false;
        }

        referenceDirectory = Path.Combine(matchingPackDirectory, "ref", targetFrameworkMoniker);

        return true;
    }

    private static string GetBaseTargetFramework(string? targetFramework)
    {
        var separatorIndex = targetFramework?.IndexOf('-') ?? -1;
        var result = separatorIndex > 0 ? targetFramework![..separatorIndex] : targetFramework ?? string.Empty;

        return result;
    }

    private static bool TryParseNetCoreVersion(string targetFramework, out Version targetVersion)
    {
        if (!targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase)
            || targetFramework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
        {
            targetVersion = new Version();

            return false;
        }

        var versionText = targetFramework[3..];
        var result = Version.TryParse(versionText, out var parsedVersion);
        targetVersion = parsedVersion ?? new Version();

        return result;
    }

    private static string? GetDotNetRoot()
    {
        var configuredRoot = WorkspaceBuildRegistration.DotNetRoot;
        if (!string.IsNullOrWhiteSpace(configuredRoot) && Directory.Exists(configuredRoot))
        {
            return configuredRoot;
        }

        var environmentRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(environmentRoot) && Directory.Exists(environmentRoot))
        {
            return environmentRoot;
        }

        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        var hostRoot = string.IsNullOrWhiteSpace(hostPath) ? null : Path.GetDirectoryName(hostPath);
        var result = !string.IsNullOrWhiteSpace(hostRoot) && Directory.Exists(hostRoot) ? hostRoot : null;

        return result;
    }
}