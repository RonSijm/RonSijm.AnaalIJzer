namespace RonSijm.AnaalIJzer.Core.Findings;

public static class ArchitecturalDiagnosticIds
{
    public const string DependencyNotAllowed = "ARCH_DEP_001";
    public const string DependencyRequiredMissing = "ARCH_DEP_002";
    public const string TypeNotAllowed = "ARCH_TYPE_001";
    public const string DependencyReverseDirection = "ARCH_DEP_004";
    public const string DependencyPeerScope = "ARCH_DEP_005";
    public const string ConfigurationRequiredMissing = "ARCH_CONF_002";
    public const string ConfigurationInvalid = "ARCH_CONF_003";
    public const string ConfigurationCycle = "ARCH_CONF_006";
    public const string NameShapeMismatch = "ARCH_NAME_008";
    public const string ApiExposureNotAllowed = "ARCH_API_001";
    public const string ProjectReferenceNotAllowed = "ARCH_PROJ_001";
    public const string PackageReferenceNotAllowed = "ARCH_PKG_001";
    public const string VisibilityNotAllowed = "ARCH_VIS_001";
    public const string ContractShapeMismatch = "ARCH_CONT_008";
    public const string ApiTransitiveExposure = "ARCH_API_010";
    public const string SourceBoundaryPlacement = "ARCH_SRC_007";
    public const string BoundaryEntryPlacement = "ARCH_BOUND_007";
    public const string ExceptionReviewLifecycle = "ARCH_EXC_009";
    public const string DependencyCycle = "ARCH_DEP_006";
    public const string DependencyReviewLifecycle = "ARCH_DEP_009";
    public const string InheritanceNotAllowed = "ARCH_INH_001";
    public const string ReturnNotAllowed = "ARCH_RET_001";
    public const string OperationNotAllowed = "ARCH_OPER_001";
    public const string OperationRequiredMissing = "ARCH_OPER_002";
    public const string OperationCardinality = "ARCH_OPER_011";
    public const string OperationOrdering = "ARCH_OPER_012";
    public const string OperationContractNotAllowed = "ARCH_OPCT_001";
    public const string OperationContractRequiredMissing = "ARCH_OPCT_002";
    public const string OperationContractShapeMismatch = "ARCH_OPCT_008";
    public const string AssemblyAttributeNotAllowed = "ARCH_ASSM_001";
    public const string AssemblyReferenceNotAllowed = "ARCH_AREF_001";
    public const string NamespaceBoundaryPlacement = "ARCH_NS_007";
    public const string SolutionReferenceNotAllowed = "ARCH_SOL_001";
    public const string SolutionCycle = "ARCH_SOL_006";
    public const string TypeRequiredMissing = "ARCH_TYPE_002";
    public const string TypeShapeMismatch = "ARCH_TYPE_008";

    public static bool IsBehavioralOperationPolicy(string? diagnosticId)
    {
        var result = diagnosticId is OperationRequiredMissing or OperationCardinality or OperationOrdering;

        return result;
    }

    public static bool IsOperationContract(string? diagnosticId)
    {
        var result = diagnosticId is OperationContractNotAllowed or OperationContractRequiredMissing or OperationContractShapeMismatch;

        return result;
    }
}