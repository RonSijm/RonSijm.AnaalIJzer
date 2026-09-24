using System.Text;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static class WorkspaceOperationContractReportAppender
{
    internal static string Append(string report, IEnumerable<ArchitectureFinding> findings)
    {
        var operationFindings = findings
            .Where(finding => finding.Code is ArchitectureFindingCodes.OperationContractOwnerMissing or ArchitectureFindingCodes.OperationContractOwnerAmbiguous)
            .OrderBy(finding => finding.Properties.TryGetValue(ArchitectureDiagnosticProperties.PropertyOperationContractName, out var operationName) ? operationName : string.Empty, StringComparer.Ordinal)
            .ThenBy(finding => finding.Code, StringComparer.Ordinal)
            .ToArray();
        if (operationFindings.Length == 0)
        {
            return report;
        }

        var normalizedReport = report.Replace("✅ **No violations found.**", "✅ **No compiler analyzer violations found.**");
        var sb = new StringBuilder(normalizedReport.TrimEnd());
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Workspace Operation Contract Findings");
        sb.AppendLine();
        sb.AppendLine("Owner cardinality is a solution-wide fact. These findings are produced by the workspace host rather than by a single-project compiler analyzer run.");
        sb.AppendLine();
        sb.AppendLine("| Operation | Finding | Context |");
        sb.AppendLine("|-----------|---------|---------|");
        foreach (var finding in operationFindings)
        {
            var operationName = GetProperty(finding, ArchitectureDiagnosticProperties.PropertyOperationContractName);
            sb.AppendLine("| `" + EscapeTable(operationName) + "` | " + EscapeTable(finding.Message) + " | " + EscapeTable(finding.Context) + " |");
        }

        var result = sb.ToString() + Environment.NewLine;

        return result;
    }

    private static string GetProperty(ArchitectureFinding finding, string propertyName)
    {
        var result = finding.Properties.TryGetValue(propertyName, out var value) ? value ?? string.Empty : string.Empty;

        return result;
    }

    private static string EscapeTable(string value)
    {
        var result = value.Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", " ").Replace("\n", " ");

        return result;
    }
}