namespace RonSijm.AnaalIJzer.Core.Statistics.Model;

public sealed class StatisticsScanFailure(string? projectPath, string stage, string message)
{
	public string? ProjectPath { get; } = projectPath;
	public string Stage { get; } = stage;
	public string Message { get; } = message;
}
