namespace RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

public static class ArchitectureDiagnosticCatalog
{
    public static ArchitectureDiagnosticDefinition DependencyNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.DependencyNotAllowed, ArchitectureDiagnosticConcern.Dependency, ArchitectureDiagnosticReason.NotAllowed, "Architectural dependency is not allowed", "diagnostics/arch_dep_001-dependency-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition DependencyRequiredMissing { get; } = Create(ArchitecturalDiagnosticIds.DependencyRequiredMissing, ArchitectureDiagnosticConcern.Dependency, ArchitectureDiagnosticReason.RequiredMissing, "Architectural dependency classification is missing", "diagnostics/arch_dep_002-dependency-classification-missing", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition TypeNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.TypeNotAllowed, ArchitectureDiagnosticConcern.Type, ArchitectureDiagnosticReason.NotAllowed, "Architectural type policy violation", "diagnostics/arch_type_001-type-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition DependencyReverseDirection { get; } = Create(ArchitecturalDiagnosticIds.DependencyReverseDirection, ArchitectureDiagnosticConcern.Dependency, ArchitectureDiagnosticReason.ReverseDirection, "Architectural dependency uses the reverse direction", "diagnostics/arch_dep_004-dependency-reverse-direction", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition DependencyPeerScope { get; } = Create(ArchitecturalDiagnosticIds.DependencyPeerScope, ArchitectureDiagnosticConcern.Dependency, ArchitectureDiagnosticReason.PeerScope, "Same-layer architectural dependency", "diagnostics/arch_dep_005-dependency-peer-scope", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ConfigurationInvalid { get; } = Create(ArchitecturalDiagnosticIds.ConfigurationInvalid, ArchitectureDiagnosticConcern.Configuration, ArchitectureDiagnosticReason.InvalidDefinition, "Invalid architecture configuration", "diagnostics/arch_conf_003-invalid-configuration", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition ConfigurationCycle { get; } = Create(ArchitecturalDiagnosticIds.ConfigurationCycle, ArchitectureDiagnosticConcern.Configuration, ArchitectureDiagnosticReason.Cycle, "Cyclic architecture dependency graph", "diagnostics/arch_conf_006-configuration-cycle", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition NameShapeMismatch { get; } = Create(ArchitecturalDiagnosticIds.NameShapeMismatch, ArchitectureDiagnosticConcern.Name, ArchitectureDiagnosticReason.ShapeMismatch, "Architectural name rule violation", "diagnostics/arch_name_008-name-shape-mismatch", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ApiExposureNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.ApiExposureNotAllowed, ArchitectureDiagnosticConcern.Api, ArchitectureDiagnosticReason.NotAllowed, "Architectural API surface exposure is not allowed", "diagnostics/arch_api_001-api-exposure-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ProjectReferenceNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed, ArchitectureDiagnosticConcern.Project, ArchitectureDiagnosticReason.NotAllowed, "Architectural project reference is not allowed", "diagnostics/arch_proj_001-project-reference-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition PackageReferenceNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.PackageReferenceNotAllowed, ArchitectureDiagnosticConcern.Package, ArchitectureDiagnosticReason.NotAllowed, "Architectural package reference is not allowed", "diagnostics/arch_pkg_001-package-reference-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition VisibilityNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.VisibilityNotAllowed, ArchitectureDiagnosticConcern.Visibility, ArchitectureDiagnosticReason.NotAllowed, "Architectural visibility is not allowed", "diagnostics/arch_vis_001-visibility-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ContractShapeMismatch { get; } = Create(ArchitecturalDiagnosticIds.ContractShapeMismatch, ArchitectureDiagnosticConcern.Contract, ArchitectureDiagnosticReason.ShapeMismatch, "Architectural contract shape mismatch", "diagnostics/arch_cont_008-contract-shape-mismatch", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ApiTransitiveExposure { get; } = Create(ArchitecturalDiagnosticIds.ApiTransitiveExposure, ArchitectureDiagnosticConcern.Api, ArchitectureDiagnosticReason.TransitiveReachability, "Forbidden transitive API exposure", "diagnostics/arch_api_010-transitive-api-exposure", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition SourceBoundaryPlacement { get; } = Create(ArchitecturalDiagnosticIds.SourceBoundaryPlacement, ArchitectureDiagnosticConcern.Source, ArchitectureDiagnosticReason.BoundaryPlacement, "Architectural source placement violation", "diagnostics/arch_src_007-source-placement", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition BoundaryEntryPlacement { get; } = Create(ArchitecturalDiagnosticIds.BoundaryEntryPlacement, ArchitectureDiagnosticConcern.Boundary, ArchitectureDiagnosticReason.BoundaryPlacement, "Architectural boundary entry-point violation", "diagnostics/arch_bound_007-boundary-entry-point", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ExceptionReviewLifecycle { get; } = Create(ArchitecturalDiagnosticIds.ExceptionReviewLifecycle, ArchitectureDiagnosticConcern.Exception, ArchitectureDiagnosticReason.ReviewLifecycle, "Architecture exception requires review", "diagnostics/arch_exc_009-exception-review", ArchitectureDiagnosticDefaultSeverity.Warning, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition DependencyCycle { get; } = Create(ArchitecturalDiagnosticIds.DependencyCycle, ArchitectureDiagnosticConcern.Dependency, ArchitectureDiagnosticReason.Cycle, "Observed architectural dependency cycle", "diagnostics/arch_dep_006-observed-dependency-cycle", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition InheritanceNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.InheritanceNotAllowed, ArchitectureDiagnosticConcern.Inheritance, ArchitectureDiagnosticReason.NotAllowed, "Architectural inheritance is not allowed", "diagnostics/arch_inh_001-inheritance-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition ReturnNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.ReturnNotAllowed, ArchitectureDiagnosticConcern.Return, ArchitectureDiagnosticReason.NotAllowed, "Architectural return value is not allowed", "diagnostics/arch_ret_001-return-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition OperationNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.OperationNotAllowed, ArchitectureDiagnosticConcern.Operation, ArchitectureDiagnosticReason.NotAllowed, "Architectural operation is not allowed", "diagnostics/arch_oper_001-operation-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition OperationRequiredMissing { get; } = Create(ArchitecturalDiagnosticIds.OperationRequiredMissing, ArchitectureDiagnosticConcern.Operation, ArchitectureDiagnosticReason.RequiredMissing, "Required architectural operation is missing", "diagnostics/arch_oper_002-required-operation-missing", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition OperationCardinality { get; } = Create(ArchitecturalDiagnosticIds.OperationCardinality, ArchitectureDiagnosticConcern.Operation, ArchitectureDiagnosticReason.Cardinality, "Architectural operation count is exceeded", "diagnostics/arch_oper_011-operation-cardinality", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition OperationOrdering { get; } = Create(ArchitecturalDiagnosticIds.OperationOrdering, ArchitectureDiagnosticConcern.Operation, ArchitectureDiagnosticReason.Ordering, "Architectural operation ordering violation", "diagnostics/arch_oper_012-operation-ordering", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition OperationContractNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.OperationContractNotAllowed, ArchitectureDiagnosticConcern.OperationContract, ArchitectureDiagnosticReason.NotAllowed, "Operation-contract participant is not allowed", "diagnostics/arch_opct_001-participant-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition OperationContractRequiredMissing { get; } = Create(ArchitecturalDiagnosticIds.OperationContractRequiredMissing, ArchitectureDiagnosticConcern.OperationContract, ArchitectureDiagnosticReason.RequiredMissing, "Required operation-contract participant is missing", "diagnostics/arch_opct_002-required-participant-missing", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition OperationContractShapeMismatch { get; } = Create(ArchitecturalDiagnosticIds.OperationContractShapeMismatch, ArchitectureDiagnosticConcern.OperationContract, ArchitectureDiagnosticReason.ShapeMismatch, "Operation-contract shape mismatch", "diagnostics/arch_opct_008-operation-contract-shape", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.All);
    public static ArchitectureDiagnosticDefinition AssemblyAttributeNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed, ArchitectureDiagnosticConcern.Assembly, ArchitectureDiagnosticReason.NotAllowed, "Architectural assembly attribute is not allowed", "diagnostics/arch_assm_001-assembly-attribute-not-allowed", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition NamespaceBoundaryPlacement { get; } = Create(ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement, ArchitectureDiagnosticConcern.Namespace, ArchitectureDiagnosticReason.BoundaryPlacement, "Namespace hierarchy dependency violation", "diagnostics/arch_ns_007-namespace-hierarchy", ArchitectureDiagnosticDefaultSeverity.Error);
    public static ArchitectureDiagnosticDefinition SolutionReferenceNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.SolutionReferenceNotAllowed, ArchitectureDiagnosticConcern.Solution, ArchitectureDiagnosticReason.NotAllowed, "Solution topology reference is not allowed", "configuration/solution-topology", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.Workspace);
    public static ArchitectureDiagnosticDefinition SolutionCycle { get; } = Create(ArchitecturalDiagnosticIds.SolutionCycle, ArchitectureDiagnosticConcern.Solution, ArchitectureDiagnosticReason.Cycle, "Solution topology cycle", "configuration/solution-topology", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.Workspace);
    public static ArchitectureDiagnosticDefinition ConfigurationRequiredMissing { get; } = Create(ArchitecturalDiagnosticIds.ConfigurationRequiredMissing, ArchitectureDiagnosticConcern.Configuration, ArchitectureDiagnosticReason.RequiredMissing, "Configured matcher has no match", "tools/arse", ArchitectureDiagnosticDefaultSeverity.Warning, ArchitectureDiagnosticSurface.Workspace);
    public static ArchitectureDiagnosticDefinition TypeRequiredMissing { get; } = Create(ArchitecturalDiagnosticIds.TypeRequiredMissing, ArchitectureDiagnosticConcern.Type, ArchitectureDiagnosticReason.RequiredMissing, "Type has no architectural classification", "tools/arse", ArchitectureDiagnosticDefaultSeverity.Warning, ArchitectureDiagnosticSurface.Workspace);
    public static ArchitectureDiagnosticDefinition TypeShapeMismatch { get; } = Create(ArchitecturalDiagnosticIds.TypeShapeMismatch, ArchitectureDiagnosticConcern.Type, ArchitectureDiagnosticReason.ShapeMismatch, "Type matches ambiguous architectural layers", "tools/arse", ArchitectureDiagnosticDefaultSeverity.Warning, ArchitectureDiagnosticSurface.Workspace);
    public static ArchitectureDiagnosticDefinition DependencyReviewLifecycle { get; } = Create(ArchitecturalDiagnosticIds.DependencyReviewLifecycle, ArchitectureDiagnosticConcern.Dependency, ArchitectureDiagnosticReason.ReviewLifecycle, "Allowed dependency requires review", "tools/arse", ArchitectureDiagnosticDefaultSeverity.Warning, ArchitectureDiagnosticSurface.Workspace);
    public static ArchitectureDiagnosticDefinition AssemblyReferenceNotAllowed { get; } = Create(ArchitecturalDiagnosticIds.AssemblyReferenceNotAllowed, ArchitectureDiagnosticConcern.AssemblyReference, ArchitectureDiagnosticReason.NotAllowed, "Architectural assembly reference is not allowed", "configuration/assembly-reference-policies", ArchitectureDiagnosticDefaultSeverity.Error, ArchitectureDiagnosticSurface.Workspace);

    public static IReadOnlyList<ArchitectureDiagnosticDefinition> All { get; } =
    [
        DependencyNotAllowed,
        DependencyRequiredMissing,
        TypeNotAllowed,
        DependencyReverseDirection,
        DependencyPeerScope,
        ConfigurationInvalid,
        ConfigurationCycle,
        NameShapeMismatch,
        ApiExposureNotAllowed,
        ProjectReferenceNotAllowed,
        PackageReferenceNotAllowed,
        VisibilityNotAllowed,
        ContractShapeMismatch,
        ApiTransitiveExposure,
        SourceBoundaryPlacement,
        BoundaryEntryPlacement,
        ExceptionReviewLifecycle,
        DependencyCycle,
        InheritanceNotAllowed,
        ReturnNotAllowed,
        OperationNotAllowed,
        OperationRequiredMissing,
        OperationCardinality,
        OperationOrdering,
        OperationContractNotAllowed,
        OperationContractRequiredMissing,
        OperationContractShapeMismatch,
        AssemblyAttributeNotAllowed,
        NamespaceBoundaryPlacement,
        SolutionReferenceNotAllowed,
        SolutionCycle,
        ConfigurationRequiredMissing,
        TypeRequiredMissing,
        TypeShapeMismatch,
        DependencyReviewLifecycle,
        AssemblyReferenceNotAllowed
    ];

    public static ArchitectureDiagnosticDefinition Get(string id)
    {
        var result = All.First(definition => string.Equals(definition.Id, id, StringComparison.Ordinal));

        return result;
    }

    public static bool TryGet(string id, out ArchitectureDiagnosticDefinition? definition)
    {
        definition = All.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
        var result = definition is not null;

        return result;
    }

    private static ArchitectureDiagnosticDefinition Create(string id, ArchitectureDiagnosticConcern concern, ArchitectureDiagnosticReason reason, string title, string documentationPath, ArchitectureDiagnosticDefaultSeverity severity, ArchitectureDiagnosticSurface surface = ArchitectureDiagnosticSurface.Compiler)
    {
        var result = new ArchitectureDiagnosticDefinition(id, concern, reason, title, documentationPath, severity, surface);

        return result;
    }
}