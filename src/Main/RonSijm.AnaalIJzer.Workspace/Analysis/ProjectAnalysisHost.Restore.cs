using System.Diagnostics;
using Microsoft.Build.Execution;
using Microsoft.Build.Locator;

namespace RonSijm.AnaalIJzer.Workspace;

internal sealed partial class ProjectAnalysisHost
{
	private static readonly object MsBuildRegistrationLock = new();
	private static readonly object RestoreLock = new();
	private static readonly HashSet<string> RestoredProjects = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> RestoredSolutions = new(StringComparer.OrdinalIgnoreCase);

	private static void EnsureRestored(string projectPath)
	{
		var assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
		if (File.Exists(assetsPath))
		{
			return;
		}

		lock (RestoreLock)
		{
			if (File.Exists(assetsPath))
			{
				return;
			}

			if (!RestoredProjects.Add(projectPath))
			{
				return;
			}

			try
			{
				TryRestoreWithDotNet(projectPath);
			}
			catch
			{
				TryRestoreWithMsBuild(projectPath);
			}

			if (!File.Exists(assetsPath))
			{
				RestoredProjects.Remove(projectPath);
				throw new InvalidOperationException("Restore did not produce " + assetsPath + ".");
			}
		}
	}

	private static void EnsureSolutionRestored(string solutionPath)
	{
		lock (RestoreLock)
		{
			if (!RestoredSolutions.Add(solutionPath))
			{
				return;
			}

			try
			{
				TryRestoreWithDotNet(solutionPath);
			}
			catch
			{
				TryRestoreWithMsBuild(solutionPath);
			}
		}
	}

	private static void TryRestoreWithMsBuild(string projectPath)
	{
		var request = new BuildRequestData(projectPath, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
		{
			["Configuration"] = "Release",
			["EnableArchitecturalLevelAnalyzer"] = "false",
			["EnableSourceLink"] = "false"
		}, null, ["Restore"], null);
		var result = BuildManager.DefaultBuildManager.Build(new BuildParameters(), request);
		if (result.OverallResult != BuildResultCode.Success)
		{
			throw new InvalidOperationException("MSBuild restore failed for " + projectPath + ".");
		}
	}

	private static void TryRestoreWithDotNet(string projectPath)
	{
		using var process = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = "restore \"" + projectPath + "\" --verbosity minimal",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		}) ?? throw new InvalidOperationException("Could not start dotnet restore for " + projectPath + ".");
		process.WaitForExit();
		if (process.ExitCode == 0)
		{
			return;
		}

		var output = process.StandardOutput.ReadToEnd();
		var error = process.StandardError.ReadToEnd();
		throw new InvalidOperationException("dotnet restore failed for " + projectPath + "." + Environment.NewLine + output + Environment.NewLine + error);
	}

	private static void RegisterMsBuild()
	{
		lock (MsBuildRegistrationLock)
		{
			if (MSBuildLocator.CanRegister)
			{
				MSBuildLocator.RegisterDefaults();
			}
		}
	}
}
