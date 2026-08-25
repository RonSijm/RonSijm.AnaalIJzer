using System.Text;

namespace RonSijm.AnaalIJzer.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
	private static void AppendArch001(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH001 — Illegal Layer Dependencies");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Dependency (layer) | Reason |");
		sb.AppendLine("|----------------|--------------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` ({violation.DepLayerName}) | {violation.ViolationReason} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch004(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH004 — Wrong-Direction Dependencies");
		sb.AppendLine();
		sb.AppendLine("The caller depends on a layer that is configured to depend on it. Reverse the dependency or invert it with an abstraction.");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Dependency (layer) | Reason |");
		sb.AppendLine("|----------------|--------------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` ({violation.DepLayerName}) | {violation.ViolationReason} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch005(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH005 — Same-Layer Dependencies");
		sb.AppendLine();
		sb.AppendLine("Types within the same layer may not depend on each other. Extract the shared concept to a lower layer or merge the responsibilities.");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Dependency | Reason |");
		sb.AppendLine("|----------------|------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` | {violation.ViolationReason} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch002(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH002 — Unrecognized Dependencies");
		sb.AppendLine();
		sb.AppendLine("These types are injected into layered callers but are not configured in `Architecture.anl`.");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Unrecognized dependency | Note |");
		sb.AppendLine("|----------------|-------------------------|------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` | {violation.Comment ?? string.Empty} |");
		}

		sb.AppendLine();

		var suggestions = violations
			.GroupBy(violation => (violation.DependencyTypeName, violation.CallerLayerName))
			.Select(group => new
			{
				TypeName = group.Key.DependencyTypeName,
				CallerLayer = group.Key.CallerLayerName,
				Suffix = ViolationRecordFactory.ExtractSuggestedLayerSuffix(group.Key.DependencyTypeName),
				Count = group.Count()
			})
			.OrderBy(suggestion => suggestion.CallerLayer)
			.ThenBy(suggestion => suggestion.Suffix)
			.ToList();

		if (suggestions.Count == 0)
		{
			return;
		}

		sb.AppendLine("---");
		sb.AppendLine();
		sb.AppendLine("## Suggested Configuration");
		sb.AppendLine();
		sb.AppendLine("Add the following to `Architecture.anl` to resolve all ARCH002 violations:");
		sb.AppendLine();
		sb.AppendLine("```xml");

		foreach (var suggestion in suggestions)
		{
			sb.AppendLine($"<!-- Resolves {suggestion.Count} violation(s) from layer '{suggestion.CallerLayer}' -->");
			sb.AppendLine($"<Layer name=\"{suggestion.Suffix}\">");
			sb.AppendLine($"    <Class endsWith=\"{suggestion.Suffix}\" />");
			sb.AppendLine("</Layer>");
			sb.AppendLine($"<AllowedDependency from=\"{suggestion.CallerLayer}\" to=\"{suggestion.Suffix}\" />");
		}

		sb.AppendLine("```");
		sb.AppendLine();
		sb.AppendLine("> **Note**: Review layer names and allowed paths before applying.");
		sb.AppendLine();
	}
}
