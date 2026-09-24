using System.Text;
using RonSijm.AnaalIJzer.Core.Findings;
using RonSijm.AnaalIJzer.Core.Violations;
using AnalyzerConfiguration = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Outputs.Violations;

internal static partial class ViolationMarkdownReportBuilder
{
    internal static string Generate(IEnumerable<ViolationRecord> violationBag, AnalyzerConfiguration config, string? inputName, string inputLabel)
    {
        var all = violationBag.ToList();

        var sb = new StringBuilder();
        AppendHeader(sb, inputName, inputLabel);
        AppendSummary(sb, all);
        if (all.Count == 0)
        {
            sb.AppendLine("✅ **No violations found.**");
            var noViolationsResult = sb.ToString();

            return noViolationsResult;
        }

        sb.AppendLine("---");
        sb.AppendLine();

        AppendDependencyNotAllowed(sb, Get(ArchitecturalDiagnosticIds.DependencyNotAllowed));
        AppendDependencyReverseDirection(sb, Get(ArchitecturalDiagnosticIds.DependencyReverseDirection));
        AppendDependencyPeerScope(sb, Get(ArchitecturalDiagnosticIds.DependencyPeerScope));
        AppendDependencyRequiredMissing(sb, Get(ArchitecturalDiagnosticIds.DependencyRequiredMissing));
        AppendTypeNotAllowed(sb, Get(ArchitecturalDiagnosticIds.TypeNotAllowed));
        AppendNameShapeMismatch(sb, Get(ArchitecturalDiagnosticIds.NameShapeMismatch));
        AppendApiExposureNotAllowed(sb, Get(ArchitecturalDiagnosticIds.ApiExposureNotAllowed));
        AppendProjectReferenceNotAllowed(sb, Get(ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed));
        AppendPackageReferenceNotAllowed(sb, Get(ArchitecturalDiagnosticIds.PackageReferenceNotAllowed));
        AppendVisibilityNotAllowed(sb, Get(ArchitecturalDiagnosticIds.VisibilityNotAllowed));
        AppendContractShapeMismatch(sb, Get(ArchitecturalDiagnosticIds.ContractShapeMismatch));
        AppendApiTransitiveExposure(sb, Get(ArchitecturalDiagnosticIds.ApiTransitiveExposure));
        AppendSourceBoundaryPlacement(sb, Get(ArchitecturalDiagnosticIds.SourceBoundaryPlacement));
        AppendBoundaryEntryPlacement(sb, Get(ArchitecturalDiagnosticIds.BoundaryEntryPlacement));
        AppendDependencyCycle(sb, Get(ArchitecturalDiagnosticIds.DependencyCycle));
        AppendInheritanceNotAllowed(sb, Get(ArchitecturalDiagnosticIds.InheritanceNotAllowed));
        AppendReturnNotAllowed(sb, Get(ArchitecturalDiagnosticIds.ReturnNotAllowed));
        AppendOperationNotAllowed(sb, Get(ArchitecturalDiagnosticIds.OperationNotAllowed));
        AppendBehavioralOperationPolicy(sb, ArchitecturalDiagnosticIds.OperationRequiredMissing, "Required Operation Missing", Get(ArchitecturalDiagnosticIds.OperationRequiredMissing));
        AppendBehavioralOperationPolicy(sb, ArchitecturalDiagnosticIds.OperationCardinality, "Operation Cardinality Violations", Get(ArchitecturalDiagnosticIds.OperationCardinality));
        AppendBehavioralOperationPolicy(sb, ArchitecturalDiagnosticIds.OperationOrdering, "Operation Ordering Violations", Get(ArchitecturalDiagnosticIds.OperationOrdering));
        AppendOperationContract(sb, ArchitecturalDiagnosticIds.OperationContractNotAllowed, "Operation-Contract Participants Not Allowed", Get(ArchitecturalDiagnosticIds.OperationContractNotAllowed));
        AppendOperationContract(sb, ArchitecturalDiagnosticIds.OperationContractRequiredMissing, "Required Operation-Contract Participants Missing", Get(ArchitecturalDiagnosticIds.OperationContractRequiredMissing));
        AppendOperationContract(sb, ArchitecturalDiagnosticIds.OperationContractShapeMismatch, "Operation-Contract Shape Mismatches", Get(ArchitecturalDiagnosticIds.OperationContractShapeMismatch));
        AppendAssemblyAttributeNotAllowed(sb, Get(ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed));
        AppendNamespaceBoundaryPlacement(sb, Get(ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement));

        var result = sb.ToString();

        return result;

        List<ViolationRecord> Get(string diagnosticId)
        {
            var result = all.Where(violation => violation.DiagnosticId == diagnosticId).ToList();

            return result;
        }
    }

    private static string EscapeTable(string value)
    {
        var result = value.Replace("|", "\\|").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r", " ").Replace("\n", " ");

        return result;
    }
}