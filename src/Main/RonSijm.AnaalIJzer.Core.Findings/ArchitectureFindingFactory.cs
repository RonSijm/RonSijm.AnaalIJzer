using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.Findings;

public static class ArchitectureFindingFactory
{
	public static ArchitectureFinding FromDiagnostic(Diagnostic diagnostic, string? context = null)
	{
		var properties = diagnostic.Properties.ToImmutableDictionary(StringComparer.Ordinal);
		var findingContext = string.IsNullOrWhiteSpace(context)
			? FormatLocation(diagnostic.Location)
			: context;
		var reasonCode = TryGetReasonCode(diagnostic.Properties);
		var state = diagnostic.Properties.TryGetValue(ArchitectureDiagnosticProperties.PropertyExceptionStatus, out var exceptionStatus)
			? exceptionStatus
			: null;
		var result = new ArchitectureFinding(
			ArchitectureFindingSeverityExtensions.FromDiagnosticSeverity(diagnostic.Severity),
			diagnostic.Id,
			diagnostic.GetMessage(),
			findingContext ?? string.Empty,
			state,
			reasonCode,
			properties);

		return result;
	}

	private static string FormatLocation(Location location)
	{
		if (location == Location.None || !location.IsInSource)
		{
			return string.Empty;
		}

		var lineSpan = location.GetLineSpan();
		var path = lineSpan.Path;
		var lineNumber = lineSpan.StartLinePosition.Line + 1;
		var result = string.IsNullOrWhiteSpace(path) ? $"line {lineNumber}" : $"{path}:{lineNumber}";

		return result;
	}

	private static string? TryGetReasonCode(IReadOnlyDictionary<string, string?> properties)
	{
		var reason = properties.TryGetValue(ArchitectureDiagnosticProperties.PropertyEntryPointFailureReason, out var entryPointReason)
			? entryPointReason
			: properties.TryGetValue(ArchitectureDiagnosticProperties.PropertyContractViolationKind, out var contractReason)
				? contractReason
				: properties.TryGetValue(ArchitectureDiagnosticProperties.PropertyNameRuleKind, out var nameRuleKind)
					? nameRuleKind
					: null;

		return reason;
	}
}
