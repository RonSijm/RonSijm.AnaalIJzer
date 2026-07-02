using System.Text;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Config.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Reporting;

/// <summary>
///     Generates a Markdown violation report from a collected set of <see cref="ViolationRecord" />s.
/// </summary>
internal static class ArchitecturalViolationReporter
{
	internal static string GenerateMarkdownReport(IEnumerable<Diagnostic> diagnostics, AnalyzerConfiguration config, string? assemblyName) =>
		GenerateMarkdownReport(diagnostics.Select(TryCreateViolationRecord).OfType<ViolationRecord>(), config, assemblyName);

	internal static string GenerateMarkdownReport(IEnumerable<ViolationRecord> violationBag, AnalyzerConfiguration config, string? assemblyName)
	{
		var all = violationBag.ToList();
		var arch001 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.IllegalLevelDependency)
			.OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch002 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.UnrecognizedDependency)
			.OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch003 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.ForbiddenDependency)
			.OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch004 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.WrongDirectionDependency)
			.OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();
		var arch005 = all.Where(v => v.DiagnosticId == ArchitecturalDiagnosticIds.SameLayerDependency)
			.OrderBy(v => v.CallerTypeName).ThenBy(v => v.DependencyTypeName).ToList();

		var sb = new StringBuilder();

		sb.AppendLine("# Architectural Violation Report");
		sb.AppendLine();
		if (assemblyName is not null)
		{
			sb.AppendLine($"**Assembly**: `{assemblyName}`  ");
		}

		sb.AppendLine($"**Generated**: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
		sb.AppendLine();
		sb.AppendLine("---");
		sb.AppendLine();

		sb.AppendLine("## Summary");
		sb.AppendLine();
		sb.AppendLine("| Rule | Violations |");
		sb.AppendLine("|------|------------|");
		sb.AppendLine($"| ARCH001 — Illegal layer dependency | {arch001.Count} |");
		sb.AppendLine($"| ARCH002 — Unrecognized dependency (strict) | {arch002.Count} |");
		sb.AppendLine($"| ARCH003 — Type policy violation | {arch003.Count} |");
		sb.AppendLine($"| ARCH004 — Wrong-direction dependency | {arch004.Count} |");
		sb.AppendLine($"| ARCH005 — Same-layer dependency | {arch005.Count} |");
		sb.AppendLine($"| **Total** | **{all.Count}** |");
		sb.AppendLine();

		if (all.Count == 0)
		{
			sb.AppendLine("✅ **No violations found.**");
			return sb.ToString();
		}

		sb.AppendLine("---");
		sb.AppendLine();

		if (arch001.Count > 0)
		{
			sb.AppendLine("## ARCH001 — Illegal Layer Dependencies");
			sb.AppendLine();
			sb.AppendLine("| Caller (layer) | Dependency (layer) | Reason |");
			sb.AppendLine("|----------------|--------------------|--------|");

			foreach (var v in arch001)
			{
				sb.AppendLine(
					$"| `{v.CallerTypeName}` ({v.CallerLayerName}) | `{v.DependencyTypeName}` ({v.DepLayerName}) | {v.ViolationReason} |");
			}

			sb.AppendLine();
		}

		if (arch004.Count > 0)
		{
			sb.AppendLine("## ARCH004 — Wrong-Direction Dependencies");
			sb.AppendLine();
			sb.AppendLine(
				"The caller depends on a layer that is configured to depend on it. Reverse the dependency or invert it with an abstraction.");
			sb.AppendLine();
			sb.AppendLine("| Caller (layer) | Dependency (layer) | Reason |");
			sb.AppendLine("|----------------|--------------------|--------|");

			foreach (var v in arch004)
			{
				sb.AppendLine(
					$"| `{v.CallerTypeName}` ({v.CallerLayerName}) | `{v.DependencyTypeName}` ({v.DepLayerName}) | {v.ViolationReason} |");
			}

			sb.AppendLine();
		}

		if (arch005.Count > 0)
		{
			sb.AppendLine("## ARCH005 — Same-Layer Dependencies");
			sb.AppendLine();
			sb.AppendLine(
				"Types within the same layer may not depend on each other. Extract the shared concept to a lower layer or merge the responsibilities.");
			sb.AppendLine();
			sb.AppendLine("| Caller (layer) | Dependency | Reason |");
			sb.AppendLine("|----------------|------------|--------|");

			foreach (var v in arch005)
			{
				sb.AppendLine(
					$"| `{v.CallerTypeName}` ({v.CallerLayerName}) | `{v.DependencyTypeName}` | {v.ViolationReason} |");
			}

			sb.AppendLine();
		}

		if (arch002.Count > 0)
		{
			sb.AppendLine("## ARCH002 — Unrecognized Dependencies");
			sb.AppendLine();
			sb.AppendLine(
				"These types are injected into layered callers but are not configured in `ArchitecturalLevels.xml`.");
			sb.AppendLine();
			sb.AppendLine("| Caller (layer) | Unrecognized dependency | Note |");
			sb.AppendLine("|----------------|-------------------------|------|");

			foreach (var v in arch002)
			{
				sb.AppendLine(
					$"| `{v.CallerTypeName}` ({v.CallerLayerName}) | `{v.DependencyTypeName}` | {v.Comment ?? string.Empty} |");
			}

			sb.AppendLine();

			var suggestions = arch002
				.GroupBy(v => (v.DependencyTypeName, v.CallerLayerName))
				.Select(g => new
				{
					TypeName = g.Key.DependencyTypeName,
					CallerLayer = g.Key.CallerLayerName,
					Suffix = ExtractSuffix(g.Key.DependencyTypeName),
					Count = g.Count()
				})
				.OrderBy(s => s.CallerLayer)
				.ThenBy(s => s.Suffix)
				.ToList();

			if (suggestions.Count > 0)
			{
				sb.AppendLine("---");
				sb.AppendLine();
				sb.AppendLine("## Suggested Configuration");
				sb.AppendLine();
				sb.AppendLine("Add the following to `ArchitecturalLevels.xml` to resolve all ARCH002 violations:");
				sb.AppendLine();
				sb.AppendLine("```xml");

				foreach (var s in suggestions)
				{
					sb.AppendLine($"<!-- Resolves {s.Count} violation(s) from layer '{s.CallerLayer}' -->");
					sb.AppendLine($"<Layer name=\"{s.Suffix}\">");
					sb.AppendLine($"    <Class endsWith=\"{s.Suffix}\" />");
					sb.AppendLine("</Layer>");
					sb.AppendLine($"<AllowedDependency from=\"{s.CallerLayer}\" to=\"{s.Suffix}\" />");
				}

				sb.AppendLine("```");
				sb.AppendLine();
				sb.AppendLine("> **Note**: Review layer names and allowed paths before applying.");
				sb.AppendLine();
			}
		}

		if (arch003.Count > 0)
		{
			sb.AppendLine("## ARCH003 — Type Policy Violations");
			sb.AppendLine();
			sb.AppendLine("These dependency types match an applicable `Forbidden` policy or fail an applicable `Allowed` policy.");
			sb.AppendLine();
			sb.AppendLine("| Caller (layer) | Dependency | Reason |");
			sb.AppendLine("|----------------|------------|--------|");

			foreach (var v in arch003)
			{
				var reason = (string.IsNullOrWhiteSpace(v.ViolationReason) ? v.Comment : v.ViolationReason) ?? string.Empty;
				sb.AppendLine(
					$"| `{v.CallerTypeName}` ({v.CallerLayerName}) | `{v.DependencyTypeName}` | {EscapeTable(reason)} |");
			}

			sb.AppendLine();
		}

		return sb.ToString();
	}

	private static string EscapeTable(string value) =>
		value.Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", " ").Replace("\n", " ");

	private static ViolationRecord? TryCreateViolationRecord(Diagnostic diagnostic)
	{
		if (diagnostic.Id is not (ArchitecturalDiagnosticIds.IllegalLevelDependency
		    or ArchitecturalDiagnosticIds.UnrecognizedDependency
		    or ArchitecturalDiagnosticIds.ForbiddenDependency
		    or ArchitecturalDiagnosticIds.WrongDirectionDependency
		    or ArchitecturalDiagnosticIds.SameLayerDependency))
		{
			return null;
		}

		var properties = diagnostic.Properties;
		var callerTypeName = Get(ArchitecturalDiagnostics.PropertyCallerTypeName) ?? "UnknownCaller";
		var callerLayerName = Get(ArchitecturalDiagnostics.PropertyCallerLayerName) ?? "UnknownLayer";
		var dependencyTypeName = Get(ArchitecturalDiagnostics.PropertyDepTypeName) ?? "UnknownDependency";
		var dependencyLayerName = Get(ArchitecturalDiagnostics.PropertyDepLayerName) ?? string.Empty;
		var violationReason = Get(ArchitecturalDiagnostics.PropertyViolationReason) ?? diagnostic.GetMessage();
		var comment = Get(ArchitecturalDiagnostics.PropertyComment);

		return new ViolationRecord(
			diagnostic.Id,
			callerTypeName,
			callerLayerName,
			dependencyTypeName,
			dependencyLayerName,
			violationReason,
			string.IsNullOrWhiteSpace(comment) ? null : comment);

		string? Get(string key) =>
			properties.TryGetValue(key, out var value) ? value : null;
	}

	private static string ExtractSuffix(string typeName)
	{
		if (typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]))
		{
			return typeName.Substring(1);
		}

		return typeName;
	}
}
