using System.Diagnostics;

namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

public sealed class GitCommandRunner
{
	public async Task<GitCommandResult> RunAsync(string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "git",
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the git executable.");
		using var cancellationRegistration = cancellationToken.Register(() => TryKillProcessTree(process));
		var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
		var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);
		var result = new GitCommandResult(process.ExitCode, await outputTask, await errorTask);

		return result;
	}

	public async Task<string> RunRequiredAsync(string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
	{
		var command = await RunAsync(workingDirectory, arguments, cancellationToken);
		if (command.ExitCode != 0)
		{
			throw new InvalidOperationException("Git " + string.Join(" ", arguments) + " failed with exit code " + command.ExitCode + "." + Environment.NewLine + command.StandardError);
		}

		var result = command.StandardOutput.Trim();

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
