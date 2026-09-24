using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

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
    private readonly ArchitectureDiagnosticDefinition? definition = ResolveDefinition(code);

    public ArchitectureFindingSeverity Severity { get; } = severity;

    public string Code { get; } = code;

    public string Category
    {
        get
        {
            var result = definition?.Category ?? Code;

            return result;
        }
    }

    public string Message { get; } = message;

    public string Context { get; } = context;

    public string? State { get; } = state;

    public string? ReasonCode { get; } = reasonCode;

    public ImmutableDictionary<string, string?> Properties { get; } = AddIdentity(properties ?? ImmutableDictionary<string, string?>.Empty, ResolveDefinition(code));

    public ArchitectureDiagnosticConcern? Concern => definition?.Concern;

    public ArchitectureDiagnosticReason? Reason => definition?.Reason;

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

    private static ImmutableDictionary<string, string?> AddIdentity(ImmutableDictionary<string, string?> properties, ArchitectureDiagnosticDefinition? definition)
    {
        if (definition is null)
        {
            return properties;
        }

        var result = properties
            .SetItem(ArchitectureDiagnosticProperties.PropertyDiagnosticConcern, definition.Concern.ToString())
            .SetItem(ArchitectureDiagnosticProperties.PropertyDiagnosticReason, definition.Reason.ToString());

        return result;
    }

    private static ArchitectureDiagnosticDefinition? ResolveDefinition(string code)
    {
        ArchitectureDiagnosticCatalog.TryGet(code, out var result);

        return result;
    }
}