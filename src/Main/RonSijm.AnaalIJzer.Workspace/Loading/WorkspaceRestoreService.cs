using System.Diagnostics;
using Microsoft.Build.Execution;

namespace RonSijm.AnaalIJzer.Workspace.Loading;

internal static class WorkspaceRestoreService
{
	private static readonly object RestoreLock = new();
	private static readonly HashSet<string> RestoredProjects = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> RestoredSolutions = new(StringComparer.OrdinalIgnoreCase);

	public static void EnsureProjectRestored(string projectPath, string configuration, WorkspaceRestoreMode restoreMode)
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
				TryRestoreWithDotNet(projectPath);
			}
			catch
			{
				TryRestoreWithMsBuild(projectPath, configuration);
			}

			if (!File.Exists(assetsPath))
			{
				RestoredProjects.Remove(cacheKey);
				throw new InvalidOperationException("Restore did not produce " + assetsPath + ".");
			}
		}
	}

	public static void EnsureSolutionRestored(string solutionPath, string configuration, WorkspaceRestoreMode restoreMode)
	{
		if (restoreMode == WorkspaceRestoreMode.Never)
		{
			return;
		}

		lock (RestoreLock)
		{
			var cacheKey = solutionPath + "\u001f" + configuration;
			if (restoreMode != WorkspaceRestoreMode.Always && !RestoredSolutions.Add(cacheKey))
			{
				return;
			}

			try
			{
				TryRestoreWithDotNet(solutionPath);
			}
			catch
			{
				TryRestoreWithMsBuild(solutionPath, configuration);
			}
		}
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
		var buildResult = BuildManager.DefaultBuildManager.Build(new BuildParameters(), request);
		if (buildResult.OverallResult != BuildResultCode.Success)
		{
			throw new InvalidOperationException("MSBuild restore failed for " + path + ".");
		}
	}

	private static void TryRestoreWithDotNet(string path)
	{
		using var process = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = "restore \"" + path + "\" --verbosity minimal",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		}) ?? throw new InvalidOperationException("Could not start dotnet restore for " + path + ".");
		var outputTask = process.StandardOutput.ReadToEndAsync();
		var errorTask = process.StandardError.ReadToEndAsync();
		process.WaitForExit();
		var output = outputTask.GetAwaiter().GetResult();
		var error = errorTask.GetAwaiter().GetResult();
		if (process.ExitCode == 0)
		{
			return;
		}

		throw new InvalidOperationException("dotnet restore failed for " + path + "." + Environment.NewLine + output + Environment.NewLine + error);
	}
}
