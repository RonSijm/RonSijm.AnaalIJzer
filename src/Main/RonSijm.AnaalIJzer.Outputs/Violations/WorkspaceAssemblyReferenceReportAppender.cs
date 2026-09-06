using System.Text;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static class WorkspaceAssemblyReferenceReportAppender
{
	internal static string Append(string report, IEnumerable<ArchitectureFinding> findings)
	{
		var assemblyFindings = findings
			.Where(finding => string.Equals(finding.Code, ArchitectureFindingCodes.AssemblyReferencePolicyViolation, StringComparison.Ordinal))
			.OrderBy(finding => GetProperty(finding, ArchitectureDiagnosticProperties.PropertySourceProjectName), StringComparer.Ordinal)
			.ThenBy(finding => GetProperty(finding, ArchitectureDiagnosticProperties.PropertyAssemblyIdentity), StringComparer.Ordinal)
			.ToArray();
		if (assemblyFindings.Length == 0)
		{
			return report;
		}

		var normalizedReport = report.Replace("✅ **No violations found.**", "✅ **No compiler analyzer violations found.**");
		var sb = new StringBuilder(normalizedReport.TrimEnd());
		sb.AppendLine();
		sb.AppendLine();
		sb.AppendLine("---");
		sb.AppendLine();
		sb.AppendLine("## Workspace Assembly Reference Policy Findings");
		sb.AppendLine();
		sb.AppendLine("These direct MSBuild `Reference` items are evaluated by the workspace host. They are intentionally not compiler `ARCHxxx` diagnostics.");
		sb.AppendLine();
		sb.AppendLine("| Source project (group) | Assembly | HintPath | Reason |");
		sb.AppendLine("|------------------------|----------|----------|--------|");
		foreach (var finding in assemblyFindings)
		{
			var sourceProject = GetProperty(finding, ArchitectureDiagnosticProperties.PropertySourceProjectName);
			var sourceGroup = GetProperty(finding, ArchitectureDiagnosticProperties.PropertySourceProjectGroup);
			var assemblyIdentity = GetProperty(finding, ArchitectureDiagnosticProperties.PropertyAssemblyIdentity);
			var hintPath = GetProperty(finding, ArchitectureDiagnosticProperties.PropertyAssemblyHintPath);
			sb.AppendLine($"| `{EscapeTable(sourceProject)}` ({EscapeTable(sourceGroup)}) | `{EscapeTable(assemblyIdentity)}` | `{EscapeTable(hintPath)}` | {EscapeTable(finding.Message)} |");
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
