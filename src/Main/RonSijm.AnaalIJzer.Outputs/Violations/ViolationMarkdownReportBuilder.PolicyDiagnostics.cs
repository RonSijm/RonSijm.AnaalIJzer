using System.Text;
using RonSijm.AnaalIJzer.Core.Violations;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
	private static void AppendArch003(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH003 — Type Policy Violations");
		sb.AppendLine();
		sb.AppendLine("These dependency types match an applicable `Forbidden` policy or fail an applicable `Allowed` policy.");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Dependency | Reason |");
		sb.AppendLine("|----------------|------------|--------|");

		foreach (var violation in violations)
		{
			var reason = (string.IsNullOrWhiteSpace(violation.ViolationReason) ? violation.Comment : violation.ViolationReason) ?? string.Empty;
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` | {EscapeTable(reason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch008(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH008 — Name Rule Violations");
		sb.AppendLine();
		sb.AppendLine("These value movements or declarations fail an applicable layer-scoped `NameRules` policy.");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Source | Target | Reason |");
		sb.AppendLine("|----------------|--------|--------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` | `{violation.DepLayerName}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch012(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH012 — Visibility Policy Violations");
		sb.AppendLine();
		sb.AppendLine("These declarations fail an applicable layer-scoped `VisibilityPolicy`.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Declaration | Target | Accessibility | Reason |");
		sb.AppendLine("|-------|-------------|--------|---------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | `{violation.DeclaredAccessibility}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch013(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH013 — Contract Purity Violations");
		sb.AppendLine();
		sb.AppendLine("These contract declarations fail an applicable layer-scoped `ContractPolicy`.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Declaration | Violation kind | Reason |");
		sb.AppendLine("|-------|-------------|----------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch015(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH015 — Layer Source-Location Violations");
		sb.AppendLine();
		sb.AppendLine("These declarations are classified into a layer whose configured `SourceLocations` policies do not permit the file location.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Type | Source file | Normalized path | Assembly | Reason |");
		sb.AppendLine("|-------|------|-------------|-----------------|----------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.CallerTypeName}` | `{EscapeTable(violation.SourceFilePath ?? string.Empty)}` | `{EscapeTable(violation.NormalizedSourcePath ?? string.Empty)}` | `{EscapeTable(violation.SourceAssemblyName ?? string.Empty)}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch016(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH016 — Boundary Entry-Point Violations");
		sb.AppendLine();
		sb.AppendLine("These dependencies passed the ordinary dependency graph but still enter a boundary through the wrong child layer or entry selector.");
		sb.AppendLine();
		sb.AppendLine("| Caller (layer) | Boundary | Entered dependency (layer) | Matched entry point | Reason |");
		sb.AppendLine("|----------------|----------|----------------------------|---------------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{EscapeTable(violation.BoundaryLayerName ?? string.Empty)}` | `{violation.DependencyTypeName}` ({violation.DepLayerName}) | `{EscapeTable(violation.MatchedEntryPoint ?? string.Empty)}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch019(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH019 — Inheritance Policy Violations");
		sb.AppendLine();
		sb.AppendLine("These declarations fail an applicable layer-scoped `InheritancePolicy`.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Declaration | Violation kind | Reason |");
		sb.AppendLine("|-------|-------------|----------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendArch020(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH020 — Return-Value Policy Violations");
		sb.AppendLine();
		sb.AppendLine("These methods directly return an expression that an applicable `ReturnValuePolicy` forbids.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Method | Expression kind | Reason |");
		sb.AppendLine("|-------|--------|----------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}
}
