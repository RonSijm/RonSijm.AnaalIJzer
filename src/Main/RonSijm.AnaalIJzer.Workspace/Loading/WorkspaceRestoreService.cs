using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal static class WorkspaceRestoreService
{
    private static readonly object RestoreLock = new();
    private static readonly HashSet<string> RestoredProjects = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> RestoredSolutions = new(StringComparer.OrdinalIgnoreCase);

    public static void EnsureProjectRestored(string projectPath, string configuration, WorkspaceRestoreMode restoreMode, CancellationToken cancellationToken = default)
    {
        if (restoreMode == WorkspaceRestoreMode.Never)
        {
            return;
        }

        var assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        if (restoreMode == WorkspaceRestoreMode.Auto && File.Exists(assetsPath))
        {
            return;
        }

        lock (RestoreLock)
        {
            if (restoreMode == WorkspaceRestoreMode.Auto && File.Exists(assetsPath))
            {
                return;
            }

            var cacheKey = projectPath + "\u001f" + configuration;
            if (restoreMode != WorkspaceRestoreMode.Always && !RestoredProjects.Add(cacheKey))
            {
                return;
            }

            try
            {
                TryRestoreWithDotNet(projectPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                RestoredProjects.Remove(cacheKey);

                throw;
            }
            catch
            {
                try
                {
                    TryRestoreWithMsBuild(projectPath, configuration);
                }
                catch
                {
                    RestoredProjects.Remove(cacheKey);

                    throw;
                }
            }

            if (!File.Exists(assetsPath))
            {
                RestoredProjects.Remove(cacheKey);
                throw new InvalidOperationException("Restore did not produce " + assetsPath + ".");
            }
        }
    }

    public static void EnsureSolutionRestored(string solutionPath, string configuration, WorkspaceRestoreMode restoreMode, CancellationToken cancellationToken = default)
    {
        if (restoreMode == WorkspaceRestoreMode.Never)
        {
            return;
        }

        if (restoreMode == WorkspaceRestoreMode.Auto && AreSolutionProjectAssetsAvailable(solutionPath))
        {
            return;
        }

        lock (RestoreLock)
        {
            if (restoreMode == WorkspaceRestoreMode.Auto && AreSolutionProjectAssetsAvailable(solutionPath))
            {
                return;
            }

            var cacheKey = solutionPath + "\u001f" + configuration;
            if (restoreMode != WorkspaceRestoreMode.Always && !RestoredSolutions.Add(cacheKey))
            {
                return;
            }

            try
            {
                TryRestoreWithDotNet(solutionPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                RestoredSolutions.Remove(cacheKey);

                throw;
            }
            catch
            {
                try
                {
                    TryRestoreWithMsBuild(solutionPath, configuration);
                }
                catch
                {
                    RestoredSolutions.Remove(cacheKey);

                    throw;
                }
            }
        }
    }

    internal static bool AreSolutionProjectAssetsAvailable(string solutionPath)
    {
        bool result;
        try
        {
            var projectPaths = ReadSolutionProjectPaths(solutionPath);
            result = projectPaths.Count > 0 && projectPaths.All(HasProjectAssets);
        }
        catch
        {
            result = false;
        }

        return result;
    }

    private static void TryRestoreWithMsBuild(string path, string configuration)
    {
        var request = new BuildRequestData(path, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Configuration"] = configuration,
            ["EnableArchitecturalLevelAnalyzer"] = "false",
            ["EnableSourceLink"] = "false",
            ["RunAnalyzersDuringBuild"] = "false"
        }, null, ["Restore"], null);
        var limits = GetFallbackBuildLimits();
        var parameters = new BuildParameters
        {
            EnableNodeReuse = limits.EnableNodeReuse,
            MaxNodeCount = limits.MaxNodeCount
        };
        var buildResult = BuildManager.DefaultBuildManager.Build(parameters, request);
        if (buildResult.OverallResult != BuildResultCode.Success)
        {
            throw new InvalidOperationException("MSBuild restore failed for " + path + ".");
        }
    }

    private static void TryRestoreWithDotNet(string path, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in CreateDotNetRestoreArguments(path))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet restore for " + path + ".");
        using var cancellationRegistration = cancellationToken.Register(() => TryKillProcessTree(process));
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExitAsync(cancellationToken).GetAwaiter().GetResult();
        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();
        if (process.ExitCode == 0)
        {
            return;
        }

        throw new InvalidOperationException("dotnet restore failed for " + path + "." + Environment.NewLine + output + Environment.NewLine + error);
    }

    internal static IReadOnlyList<string> CreateDotNetRestoreArguments(string path)
    {
        string[] result = [
            "restore",
            path,
            "--verbosity",
            "minimal",
            "--disable-parallel",
            "--disable-build-servers",
            "-p:UseSharedCompilation=false"
        ];

        return result;
    }

    internal static WorkspaceRestoreBuildLimits GetFallbackBuildLimits()
    {
        var result = new WorkspaceRestoreBuildLimits(EnableNodeReuse: false, MaxNodeCount: 1);

        return result;
    }

    internal static IReadOnlyList<string> ReadSolutionProjectPaths(string solutionPath)
    {
        var extension = Path.GetExtension(solutionPath);
        IReadOnlyList<string> result;
        if (extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            result = ReadSlnxProjectPaths(solutionPath);
        }
        else
        {
            WorkspaceBuildRegistration.EnsureRegistered();
            result = ReadSlnProjectPaths(solutionPath);
        }

        return result;
    }

    private static IReadOnlyList<string> ReadSlnxProjectPaths(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var document = XDocument.Load(solutionPath, LoadOptions.None);
        var result = document
            .Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => IsRestorableProjectPath(path))
            .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path!)))
            .ToArray();

        return result;
    }

    private static IReadOnlyList<string> ReadSlnProjectPaths(string solutionPath)
    {
        var result = SolutionFile.Parse(solutionPath)
            .ProjectsInOrder
            .Select(project => project.AbsolutePath)
            .Where(IsRestorableProjectPath)
            .Select(Path.GetFullPath)
            .ToArray();

        return result;
    }

    private static bool IsRestorableProjectPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        var result = extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                     || extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase)
                     || extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private static bool HasProjectAssets(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath);
        var result = projectDirectory is not null && File.Exists(Path.Combine(projectDirectory, "obj", "project.assets.json"));

        return result;
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Cancellation cleanup is best effort. The original cancellation remains authoritative.
        }
    }
}

internal readonly record struct WorkspaceRestoreBuildLimits(bool EnableNodeReuse, int MaxNodeCount);