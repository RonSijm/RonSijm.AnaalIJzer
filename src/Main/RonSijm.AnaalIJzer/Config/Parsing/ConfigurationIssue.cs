namespace RonSijm.AnaalIJzer.Parsing;

internal enum ConfigurationIssueKind
{
	InvalidConfiguration,
	CyclicDependencyGraph
}

internal readonly struct ConfigurationIssue(ConfigurationIssueKind kind, string message, string path, int lineNumber, int linePosition)
{
	public ConfigurationIssueKind Kind { get; } = kind;
	public string Message { get; } = message;
	public string Path { get; } = path;
	public int LineNumber { get; } = lineNumber;
	public int LinePosition { get; } = linePosition;
}
