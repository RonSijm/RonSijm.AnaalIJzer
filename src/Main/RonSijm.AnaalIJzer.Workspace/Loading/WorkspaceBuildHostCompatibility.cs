using System.Reflection;
using Microsoft.CodeAnalysis.MSBuild;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal static class WorkspaceBuildHostCompatibility
{
    private const string BuildHostAssemblyName = "Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.dll";

    public static void EnsureCompatible(string applicationDirectory)
    {
        var workspaceVersion = typeof(MSBuildWorkspace).Assembly.GetName().Version;
        var buildHostPath = Path.Combine(applicationDirectory, "BuildHost-netcore", BuildHostAssemblyName);
        if (workspaceVersion is null || !File.Exists(buildHostPath))
        {
            return;
        }

        var buildHostVersion = AssemblyName.GetAssemblyName(buildHostPath).Version;
        if (buildHostVersion is null || AreCompatible(workspaceVersion, buildHostVersion))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The Roslyn MSBuildWorkspace client ({workspaceVersion}) and BuildHost ({buildHostVersion}) do not match. " +
            "Run 'dotnet clean' and rebuild the application before loading a project or solution.");
    }

    internal static bool AreCompatible(Version workspaceVersion, Version buildHostVersion)
    {
        var result = workspaceVersion.Major == buildHostVersion.Major
                     && workspaceVersion.Minor == buildHostVersion.Minor;

        return result;
    }
}