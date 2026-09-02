using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

public enum ConfigurationIssueKind
{
	InvalidConfiguration,
	CyclicDependencyGraph
}

public readonly struct ConfigurationIssue(
	ConfigurationIssueKind kind,
	string message,
	string path,
	int lineNumber,
	int linePosition,
	ImmutableDictionary<string, string?>? properties = null)
{
	public ConfigurationIssueKind Kind { get; } = kind;
	public string Message { get; } = message;
	public string Path { get; } = path;
	public int LineNumber { get; } = lineNumber;
	public int LinePosition { get; } = linePosition;
	public ImmutableDictionary<string, string?> Properties { get; } = properties ?? ImmutableDictionary<string, string?>.Empty;
}
