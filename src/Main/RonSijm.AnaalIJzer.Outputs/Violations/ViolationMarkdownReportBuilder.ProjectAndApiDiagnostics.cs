using System.Text;
using RonSijm.AnaalIJzer.Core.Violations;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
    private static void AppendApiExposureNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
    {
        if (violations.Count == 0)
        {
            return;
        }

        sb.AppendLine("## ARCH_API_001 — API Exposure Not Allowed");
        sb.AppendLine();
        sb.AppendLine("These externally visible declarations expose types rejected by an applicable layer-scoped `ApiSurface` policy.");
        sb.AppendLine();
        sb.AppendLine("| Layer | API member | Exposed type (layer) | Site | Reason |");
        sb.AppendLine("|-------|------------|----------------------|------|--------|");

        foreach (var violation in violations)
        {
            sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.ApiMemberName}` | `{violation.DependencyTypeName}` (`{violation.DepLayerName}`) | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
        }

        sb.AppendLine();
    }

    private static void AppendProjectReferenceNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
    {
        if (violations.Count == 0)
        {
            return;
        }

        sb.AppendLine("## ARCH_PROJ_001 — Project References Not Allowed");
        sb.AppendLine();
        sb.AppendLine("These project-to-project references fail the configured `ProjectArchitecture` policy.");
        sb.AppendLine();
        sb.AppendLine("| Source project (group) | Target project (group) | Reason |");
        sb.AppendLine("|------------------------|------------------------|--------|");

        foreach (var violation in violations)
        {
            sb.AppendLine($"| `{violation.SourceProjectName}` ({violation.SourceProjectGroup}) | `{violation.TargetProjectName}` ({violation.TargetProjectGroup}) | {EscapeTable(violation.ViolationReason)} |");
        }

        sb.AppendLine();
    }

    private static void AppendPackageReferenceNotAllowed(StringBuilder sb, List<ViolationRecord> violations)
    {
        if (violations.Count == 0)
        {
            return;
        }

        sb.AppendLine("## ARCH_PKG_001 — Package References Not Allowed");
        sb.AppendLine();
        sb.AppendLine("These resolved NuGet package references fail the configured `ProjectArchitecture` package policies.");
        sb.AppendLine();
        sb.AppendLine("| Source project (group) | Package | Version | Kind | Reason |");
        sb.AppendLine("|------------------------|---------|---------|------|--------|");

        foreach (var violation in violations)
        {
            sb.AppendLine($"| `{violation.SourceProjectName}` ({violation.SourceProjectGroup}) | `{violation.PackageId}` | `{violation.PackageVersion}` | `{violation.PackageReferenceKind}` | {EscapeTable(violation.ViolationReason)} |");
        }

        sb.AppendLine();
    }

    private static void AppendApiTransitiveExposure(StringBuilder sb, List<ViolationRecord> violations)
    {
        if (violations.Count == 0)
        {
            return;
        }

        sb.AppendLine("## ARCH_API_010 — Transitive API Exposure");
        sb.AppendLine();
        sb.AppendLine("These externally visible declarations expose a permitted root type whose public object graph reaches a type rejected by an applicable `ApiSurface` policy.");
        sb.AppendLine();
        sb.AppendLine("| Layer | Root API member | Exposure path | Depth | Nested site | Reason |");
        sb.AppendLine("|-------|-----------------|---------------|-------|-------------|--------|");

        foreach (var violation in violations)
        {
            sb.AppendLine($"| `{violation.CallerLayerName}` | `{violation.ApiMemberName}` | `{EscapeTable(violation.ExposurePath ?? string.Empty)}` | {violation.ExposureDepth} | `{violation.DeclarationTarget}` | {EscapeTable(violation.ViolationReason)} |");
        }

        sb.AppendLine();
    }

    private static void AppendDependencyCycle(StringBuilder sb, List<ViolationRecord> violations)
    {
        if (violations.Count == 0)
        {
            return;
        }

        sb.AppendLine("## ARCH_DEP_006 — Observed Dependency Cycles");
        sb.AppendLine();
        sb.AppendLine("These cycles come from dependencies actually present in source code, not merely from configured AllowedDependency edges.");
        sb.AppendLine();
        sb.AppendLine("| Scope | Cycle | Length | Sites | Project |");
        sb.AppendLine("|-------|-------|--------|-------|---------|");

        foreach (var violation in violations)
        {
            sb.AppendLine($"| `{EscapeTable(violation.CycleScope ?? string.Empty)}` | `{EscapeTable(violation.ViolationReason)}` | {violation.CycleLength} | `{EscapeTable(violation.ObservedSites ?? string.Empty)}` | `{EscapeTable(violation.SourceProjectName ?? string.Empty)}` |");
        }

        sb.AppendLine();
    }
}