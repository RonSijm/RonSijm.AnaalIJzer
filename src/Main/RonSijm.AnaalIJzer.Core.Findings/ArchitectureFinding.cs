using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Core.Findings;

public sealed class ArchitectureFinding(
	ArchitectureFindingSeverity severity,
	string code,
	string message,
	string context,
	string? state = null,
	string? reasonCode = null,
	ImmutableDictionary<string, string?>? properties = null)
{
	public ArchitectureFindingSeverity Severity { get; } = severity;

	public string Code { get; } = code;

	public string Category
	{
		get
		{
			var result = Code;

			return result;
		}
	}

	public string Message { get; } = message;

	public string Context { get; } = context;

	public string? State { get; } = state;

	public string? ReasonCode { get; } = reasonCode;

	public ImmutableDictionary<string, string?> Properties { get; } = properties ?? ImmutableDictionary<string, string?>.Empty;

	public string SeverityText
	{
		get
		{
			var result = Severity.ToDisplayText();

			return result;
		}
	}

	public ArchitectureFinding WithContext(string context)
	{
		var result = new ArchitectureFinding(Severity, Code, Message, context, State, ReasonCode, Properties);

		return result;
	}

	public ArchitectureFinding WithContextPrefix(string prefix)
	{
		var context = string.IsNullOrWhiteSpace(Context)
			? prefix
			: $"{prefix} - {Context}";
		var result = WithContext(context);

		return result;
	}
}
