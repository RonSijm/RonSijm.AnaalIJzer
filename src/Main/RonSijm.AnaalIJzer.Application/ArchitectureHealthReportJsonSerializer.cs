using System.Text.Json;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Outputs.Inspection;

namespace RonSijm.AnaalIJzer.Application;

internal static class ArchitectureHealthReportJsonSerializer
{
	public static string Serialize(ArchitectureHealthReport report, ApplicationInputKind? inputKind, IReadOnlyList<string> inputPaths)
	{
		var document = new
		{
			schemaVersion = 1,
			inputKind = inputKind?.ToString(),
			inputPaths = inputPaths.Select(Path.GetFullPath).ToArray(),
			findingCount = report.FindingCount,
			errors = report.Findings.Count(finding => finding.Severity == ArchitectureFindingSeverity.Error),
			warnings = report.Findings.Count(finding => finding.Severity != ArchitectureFindingSeverity.Error),
			findings = report.Findings
				.OrderByDescending(finding => finding.Severity == ArchitectureFindingSeverity.Error)
				.ThenBy(finding => finding.Concern?.ToString() ?? "Other", StringComparer.Ordinal)
				.ThenBy(finding => finding.Reason?.ToString() ?? "Other", StringComparer.Ordinal)
				.ThenBy(finding => finding.Code, StringComparer.Ordinal)
				.ThenBy(finding => finding.Message, StringComparer.Ordinal)
				.Select(finding => new
				{
					severity = finding.SeverityText,
					concern = finding.Concern?.ToString(),
					reason = finding.Reason?.ToString(),
					code = finding.Code,
					message = finding.Message,
					context = finding.Context,
					state = finding.State,
					reasonCode = finding.ReasonCode,
					properties = finding.Properties
						.OrderBy(property => property.Key, StringComparer.Ordinal)
						.ToDictionary(property => property.Key, property => property.Value, StringComparer.Ordinal)
				})
				.ToArray()
		};
		var result = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });

		return result;
	}
}
