namespace RonSijm.AnaalIJzer.Statistics.GitHistory.Git;

public sealed class GitCommandResult(int exitCode, string standardOutput, string standardError)
{
	public int ExitCode { get; } = exitCode;
	public string StandardOutput { get; } = standardOutput;
	public string StandardError { get; } = standardError;
}
