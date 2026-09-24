using System.Text;
using RonSijm.AnaalIJzer.Core.Findings.Diagnostics;
using RonSijm.AnaalIJzer.Core.Violations;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
    private static void AppendHeader(StringBuilder sb, string? inputName, string inputLabel)
    {
        sb.AppendLine("# Architectural Violation Report");
        sb.AppendLine();
        if (inputName is not null)
        {
            sb.AppendLine($"**{inputLabel}**: `{inputName}`  ");
        }

        sb.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void AppendSummary(StringBuilder sb, IReadOnlyCollection<ViolationRecord> violations)
    {
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Concern | Reason | Rule | Violations |");
        sb.AppendLine("|---------|--------|------|------------|");

        foreach (var group in violations
            .GroupBy(violation => violation.DiagnosticId)
            .Select(group => new { Definition = ArchitectureDiagnosticCatalog.Get(group.Key), Count = group.Count() })
            .OrderBy(item => item.Definition.Concern)
            .ThenBy(item => item.Definition.Reason))
        {
            sb.AppendLine($"| {group.Definition.Concern} | {group.Definition.Reason} | `{group.Definition.Id}` — {group.Definition.Title} | {group.Count} |");
        }

        sb.AppendLine($"| | | **Total** | **{violations.Count}** |");
        sb.AppendLine();
    }
}