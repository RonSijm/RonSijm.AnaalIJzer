using System.Text;
using RonSijm.AnaalIJzer.Core.Violations;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
	private static void AppendTypeNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_TYPE_001 — Types Not Allowed");
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

	private static void AppendNameShapeMismatch(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_NAME_008 — Name Shape Mismatches");
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

	private static void AppendVisibilityNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_VIS_001 — Visibility Not Allowed");
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

	private static void AppendContractShapeMismatch(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_CONT_008 — Contract Shape Mismatches");
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

	private static void AppendSourceBoundaryPlacement(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_SRC_007 — Source Boundary Placement Violations");
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

	private static void AppendBoundaryEntryPlacement(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_BOUND_007 — Boundary Entry Placement Violations");
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

	private static void AppendInheritanceNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_INH_001 — Inheritance Not Allowed");
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

	private static void AppendReturnNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_RET_001 — Return Values Not Allowed");
		sb.AppendLine();
		sb.AppendLine("These methods directly return an expression that an applicable `ReturnValuePolicy` rejects.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Method | Expression kind | Reason |");
		sb.AppendLine("|-------|--------|----------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendOperationNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_OPER_001 — Operations Not Allowed");
		sb.AppendLine();
		sb.AppendLine("These selected resolved operations are forbidden by an applicable layer-scoped `ForbiddenOperations` policy.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Caller | Operation | Kind | Reason |");
		sb.AppendLine("|-------|--------|-----------|------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.CallerTypeName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendBehavioralOperationPolicy(StringBuilder sb, string diagnosticId, string title, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine($"## {diagnosticId} — {title}");
		sb.AppendLine();
		sb.AppendLine("These selected declaration bodies fail a mechanically provable operation presence, ordering, or count policy.");
		sb.AppendLine();
		sb.AppendLine("| Layer | Caller | Operation or rule | Violation kind | Reason |");
		sb.AppendLine("|-------|--------|-------------------|----------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.CallerTypeName}` | `{violation.DependencyTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendOperationContract(StringBuilder sb, string diagnosticId, string title, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine($"## {diagnosticId} — {title}");
		sb.AppendLine();
		sb.AppendLine("These selected owner or entry-point methods do not satisfy an explicit operation contract.");
		sb.AppendLine();
		sb.AppendLine("| Operation | Layer | Declaration | Violation kind | Reason |");
		sb.AppendLine("|-----------|-------|-------------|----------------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.DependencyTypeName}` | `{violation.CallerLayerName}` | `{violation.CallerTypeName}` | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendAssemblyAttributeNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_ASSM_001 — Assembly Attributes Not Allowed");
		sb.AppendLine();
		sb.AppendLine("These compiled assembly attributes do not satisfy a configured assembly attribute policy.");
		sb.AppendLine();
		sb.AppendLine("| Assembly | Attribute | Rule | Reason |");
		sb.AppendLine("|----------|-----------|------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` | `{violation.DependencyTypeName}` | `{violation.DepLayerName}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}

	private static void AppendNamespaceBoundaryPlacement(StringBuilder sb, List<ViolationRecord> violations)
	{
		if (violations.Count == 0)
		{
			return;
		}

		sb.AppendLine("## ARCH_NS_007 — Namespace Boundary Placement Violations");
		sb.AppendLine();
		sb.AppendLine("These semantic references cross a namespace ownership direction blocked by a root-level `NamespaceHierarchyPolicy`.");
		sb.AppendLine();
		sb.AppendLine("| Caller (namespace) | Dependency (namespace) | Site | Reason |");
		sb.AppendLine("|--------------------|------------------------|------|--------|");

		foreach (var violation in violations)
		{
			sb.AppendLine($"| `{violation.CallerTypeName}` ({violation.CallerLayerName}) | `{violation.DependencyTypeName}` ({violation.DepLayerName}) | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
		}

		sb.AppendLine();
	}
}
