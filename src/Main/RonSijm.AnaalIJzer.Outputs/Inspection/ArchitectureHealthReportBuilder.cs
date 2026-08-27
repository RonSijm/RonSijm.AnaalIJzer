using System.Collections.Immutable;
using System.Text;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Outputs.Inspection;

internal static class ArchitectureHealthReportBuilder
{
	public static ArchitectureHealthReport Build(string? title, IReadOnlyList<ArchitectureFinding> findings, string? inputPath, string inputPathLabel = "Project")
	{
		var errors = findings.Count(finding => finding.Severity == ArchitectureFindingSeverity.Error);
		var warnings = findings.Count - errors;
		var sb = new StringBuilder();
		sb.AppendLine("# Architecture Health");
		sb.AppendLine();
		if (!string.IsNullOrWhiteSpace(title))
		{
			sb.AppendLine($"**Input**: `{Escape(title)}`");
		}
		if (!string.IsNullOrWhiteSpace(inputPath))
		{
			sb.AppendLine($"**{inputPathLabel}**: `{Escape(inputPath)}`");
		}
		sb.AppendLine($"**Findings**: {errors} error(s), {warnings} warning(s)");
		sb.AppendLine();

		if (findings.Count == 0)
		{
			sb.AppendLine("No configuration, classification, dependency-graph, or rule-usage problems were found.");
			var emptyResult = new ArchitectureHealthReport(sb.ToString(), 0, []);

			return emptyResult;
		}

		sb.AppendLine("| Severity | Category | Finding | Context |");
		sb.AppendLine("|----------|----------|---------|---------|");
		foreach (var finding in findings.OrderByDescending(finding => finding.Severity == ArchitectureFindingSeverity.Error).ThenBy(finding => finding.Code, StringComparer.Ordinal).ThenBy(finding => finding.Message, StringComparer.Ordinal))
		{
			sb.AppendLine($"| {EscapeTable(finding.SeverityText)} | {EscapeTable(finding.Code)} | {EscapeTable(finding.Message)} | {EscapeTable(finding.Context)} |");
		}

		var result = new ArchitectureHealthReport(
			sb.ToString(),
			findings.Count,
			[..findings]);

		return result;
	}

	private static string Escape(string? text)
	{
		var result = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("`", "\\`");

		return result;
	}

	private static string EscapeTable(string? text)
	{
		var result = Escape(text).Replace("|", "\\|");

		return result;
	}
}
