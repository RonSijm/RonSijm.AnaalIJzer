using System.Collections.Immutable;

namespace RonSijm.AnaalIJzer.Findings;

public sealed class ArchitectureFinding
{
	public ArchitectureFinding(
		ArchitectureFindingSeverity severity,
		string code,
		string message,
		string context,
		string? state = null,
		string? reasonCode = null,
		ImmutableDictionary<string, string?>? properties = null)
	{
		Severity = severity;
		Code = code;
		Message = message;
		Context = context;
		State = state;
		ReasonCode = reasonCode;
		Properties = properties ?? ImmutableDictionary<string, string?>.Empty;
	}

	public ArchitectureFindingSeverity Severity { get; }

	public string Code { get; }

	public string Category
	{
		get
		{
			var result = Code;

			return result;
		}
	}

	public string Message { get; }

	public string Context { get; }

	public string? State { get; }

	public string? ReasonCode { get; }

	public ImmutableDictionary<string, string?> Properties { get; }

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
