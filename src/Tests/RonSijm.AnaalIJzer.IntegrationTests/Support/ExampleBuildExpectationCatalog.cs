using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

internal static class ExampleBuildExpectationCatalog
{
    public static IReadOnlyList<ExampleBuildExpectation> All { get; } =
    [
        Expect("Diagnostics/DEP/Example.Arch_DEP_001.GenericTypeArgument", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 3)),
        Expect("Diagnostics/DEP/Example.Arch_DEP_001.NoEdge", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Diagnostics/DEP/Example.Arch_DEP_001.NonConstructorInjection", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 8)),
        Expect("Diagnostics/DEP/Example.Arch_DEP_001.SkipsLayer", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        Expect("Diagnostics/DEP/Example.Arch_DEP_002.UnrecognizedDependency", (ArchitecturalDiagnosticIds.DependencyRequiredMissing, 1)),
        Expect("Diagnostics/TYPE/Example.Arch_TYPE_001.ForbiddenType", (ArchitecturalDiagnosticIds.TypeNotAllowed, 1)),
        Expect("Diagnostics/DEP/Example.Arch_DEP_004.WrongDirection", (ArchitecturalDiagnosticIds.DependencyReverseDirection, 1)),
        Expect("Diagnostics/DEP/Example.Arch_DEP_005.SameLayer", (ArchitecturalDiagnosticIds.DependencyPeerScope, 1)),
        Expect("Diagnostics/CONF/Example.Arch_CONF_003.UnknownLayer", (ArchitecturalDiagnosticIds.ConfigurationInvalid, 1)),
        Expect("Diagnostics/CONF/Example.Arch_CONF_006.CyclicGraph", (ArchitecturalDiagnosticIds.ConfigurationCycle, 1)),
        Expect("Diagnostics/API/Example.Arch_API_001.ApiSurfaceLeakage", (ArchitecturalDiagnosticIds.ApiExposureNotAllowed, 1)),
        Expect("Diagnostics/VIS/Example.Arch_VIS_001.VisibilityPolicy", (ArchitecturalDiagnosticIds.VisibilityNotAllowed, 1)),
        Expect("Diagnostics/CONT/Example.Arch_CONT_008.ContractPurity", (ArchitecturalDiagnosticIds.ContractShapeMismatch, 1)),
        Expect("Diagnostics/CONT/Example.Arch_CONT_008.ContractPurity.MethodBodyNotAllowed", (ArchitecturalDiagnosticIds.ContractShapeMismatch, 1)),
        Expect("Diagnostics/API/Example.Arch_API_010.TransitiveExposure", (ArchitecturalDiagnosticIds.ApiTransitiveExposure, 1)),
        ExpectFile("Diagnostics/BOUND/Example.Arch_BOUND_007.BoundaryEntryPoints", (ArchitecturalDiagnosticIds.BoundaryEntryPlacement, 1)),
        ExpectFile("Diagnostics/DEP/Example.Arch_DEP_006.ObservedCycle", (ArchitecturalDiagnosticIds.DependencyCycle, 1)),
        Expect("Diagnostics/INH/Example.Arch_INH_001.InheritancePolicy", (ArchitecturalDiagnosticIds.InheritanceNotAllowed, 1)),
        Expect("Diagnostics/RET/Example.Arch_RET_001.ExplicitNullReturn", (ArchitecturalDiagnosticIds.ReturnNotAllowed, 1)),
        Expect("Diagnostics/RET/Example.Arch_RET_001.AnnotatedInvocationReturn", (ArchitecturalDiagnosticIds.ReturnNotAllowed, 1)),
        Expect("Diagnostics/RET/Example.Arch_RET_001.ConfiguredLiteralReturns", (ArchitecturalDiagnosticIds.ReturnNotAllowed, 3)),
        Expect("Diagnostics/RET/Example.Arch_RET_001.DirectInvocationReturn", (ArchitecturalDiagnosticIds.ReturnNotAllowed, 1)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_001.BlockingTaskAccess", (ArchitecturalDiagnosticIds.OperationNotAllowed, 2)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_001.ClockAccess", (ArchitecturalDiagnosticIds.OperationNotAllowed, 3)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_001.SelectedEnvironmentMember", (ArchitecturalDiagnosticIds.OperationNotAllowed, 1)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_001.ServiceLocation", (ArchitecturalDiagnosticIds.OperationNotAllowed, 1)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_002.RequiredOperation", (ArchitecturalDiagnosticIds.OperationRequiredMissing, 1)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_012.RequiredOperationBefore", (ArchitecturalDiagnosticIds.OperationOrdering, 1)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_012.ForbiddenOperationAfter", (ArchitecturalDiagnosticIds.OperationOrdering, 1)),
        Expect("Diagnostics/OPER/Example.Arch_OPER_011.MaximumOperationCount", (ArchitecturalDiagnosticIds.OperationCardinality, 1)),
        ExpectFile("Diagnostics/OPCT/Example.Arch_OPCT_001.ParticipantNotAllowed", (ArchitecturalDiagnosticIds.OperationContractNotAllowed, 1)),
        ExpectFile("Diagnostics/OPCT/Example.Arch_OPCT_002.RequiredOwnerInvocation", (ArchitecturalDiagnosticIds.OperationContractRequiredMissing, 1)),
        ExpectFile("Diagnostics/OPCT/Example.Arch_OPCT_008.ResponseShapeMismatch", (ArchitecturalDiagnosticIds.OperationContractShapeMismatch, 1)),
        ExpectFile("Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Code", (ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed, 1)),
        ExpectFile("Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Project", (ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed, 1)),
        ExpectFile("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant", (ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement, 1)),
        ExpectFile("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor", (ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement, 1)),
        ExpectFile("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SameNamespace", (ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement, 1)),
        ExpectFile("Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling", (ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement, 1)),
        ExpectFile("Features/Example.AllowedSites", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 26)),
        Expect("Features/Example.AllowedTypes", (ArchitecturalDiagnosticIds.TypeNotAllowed, 1)),
        ExpectFile("Features/Example.ArchitectureHealth"),
        Expect("Features/Example.AssemblyMatcher", (ArchitecturalDiagnosticIds.DependencyReverseDirection, 1)),
        Expect("Features/Example.BlockedDependency", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Features/Example.CascadingDependencyRules"),
        Expect("Features/Example.CombinedMatchers", (ArchitecturalDiagnosticIds.DependencyPeerScope, 1)),
        Expect("Features/Example.DeclarationObservationMatchers", (ArchitecturalDiagnosticIds.InheritanceNotAllowed, 2)),
        Expect("Features/Example.DeclarationNameMatchesType", (ArchitecturalDiagnosticIds.NameShapeMismatch, 6)),
        ExpectFile("Features/Example.GeneratedCode", (ArchitecturalDiagnosticIds.OperationNotAllowed, 1)),
        Expect("Features/Example.ExceptionPolicy", (ArchitecturalDiagnosticIds.ExceptionReviewLifecycle, 1)),
        Expect("Features/Example.Exceptions", (ArchitecturalDiagnosticIds.TypeNotAllowed, 1)),
        ExpectFile("Features/Example.GlobalReturnValuePolicy", (ArchitecturalDiagnosticIds.ReturnNotAllowed, 1)),
        ExpectFile("Features/Example.IncludeSettings", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Features/Example.IncludeWildcardSettings", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        Expect("Features/Example.InlineXml", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        Expect("Features/Example.LayerScopedRecognizedDependencies", (ArchitecturalDiagnosticIds.DependencyRequiredMissing, 1)),
        Expect("Features/Example.NameRuleIntraProceduralTracking", (ArchitecturalDiagnosticIds.NameShapeMismatch, 3)),
        Expect("Features/Example.NameRuleLanguageForms", (ArchitecturalDiagnosticIds.NameShapeMismatch, 8)),
        Expect("Features/Example.NameRules", (ArchitecturalDiagnosticIds.NameShapeMismatch, 4)),
        Expect("Features/Example.NestedExceptions", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 2)),
        ExpectFile("Features/Example.NestedLayers"),
        ExpectFile("Features/Example.NamespaceHierarchySites", (ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement, 26)),
        Expect("Features/Example.NonClassCallers", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 3)),
        ExpectFile("Features/Example.RequiredRecognizedDependencySites", (ArchitecturalDiagnosticIds.DependencyRequiredMissing, 13)),
        Expect("Features/Example.SameLayerInheritance", (ArchitecturalDiagnosticIds.DependencyPeerScope, 1)),
        Expect("Features/Example.ScopedTypePolicies", (ArchitecturalDiagnosticIds.TypeNotAllowed, 2)),
        ExpectFile("Features/Example.SourceLocations", (ArchitecturalDiagnosticIds.SourceBoundaryPlacement, 1)),
        ExpectFile("Features/Example.StructuralDeclarationMatchers", (ArchitecturalDiagnosticIds.InheritanceNotAllowed, 1)),
        ExpectFile("Scenarios/Example.AssemblyReferenceBoundaries/Example.AssemblyReferenceBoundaries.Domain"),
        ExpectFile("Scenarios/Example.AspNetCore/Example.AspNetCore.ApiSurface", (ArchitecturalDiagnosticIds.ApiExposureNotAllowed, 1)),
        ExpectFile("Scenarios/Example.AspNetCore/Example.AspNetCore.LayerBoundaries", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Scenarios/Example.AspNetCore/Example.AspNetCore.ModelBindingNames", (ArchitecturalDiagnosticIds.NameShapeMismatch, 2)),
        ExpectFile("Scenarios/Example.AspNetCore/Example.AspNetCore.OperationContracts", (ArchitecturalDiagnosticIds.OperationContractRequiredMissing, 1)),
        ExpectFile("Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.ContextBoundary", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.ContextCreation", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.DomainPurity", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.MigrationPlacement", (ArchitecturalDiagnosticIds.SourceBoundaryPlacement, 1)),
        ExpectFile("Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.ModelConfigurationPlacement", (ArchitecturalDiagnosticIds.SourceBoundaryPlacement, 1)),
        ExpectFile("Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.QuerySurface", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1)),
        ExpectFile("Scenarios/Example.HonestTypeEndpointNames", (ArchitecturalDiagnosticIds.NameShapeMismatch, 2)),
        ExpectFile("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Data"),
        ExpectFile("Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Domain", (ArchitecturalDiagnosticIds.PackageReferenceNotAllowed, 1)),
        ExpectFile("Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Application"),
        ExpectFile("Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Domain", (ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed, 1)),
        ExpectFile("Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Infrastructure"),
        ExpectFile("Scenarios/Example.ProjectReferenceRuleSelectors/Example.ProjectReferenceRuleSelectors.Orders.Application", (ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed, 1)),
        ExpectFile("Scenarios/Example.ProjectReferenceRuleSelectors/Example.ProjectReferenceRuleSelectors.Orders.Contracts"),
        ExpectFile("Scenarios/Example.ProjectReferenceRuleSelectors/Example.ProjectReferenceRuleSelectors.Payments.Contracts"),
        ExpectFile("Scenarios/Example.RepositoryQuerySurface", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 2)),
        ExpectFile("Scenarios/Example.SolutionTopology/Example.SolutionTopology.Web"),
        ExpectFile("Scenarios/Example.SolutionTopology/Example.SolutionTopology.Application"),
        ExpectFile("Scenarios/Example.SolutionTopology/Example.SolutionTopology.Infrastructure"),
        ExpectFile("Documentation/Example.DocumentationDemo", (ArchitecturalDiagnosticIds.DependencyRequiredMissing, 1)),
        Expect("Documentation/Example.ReportDemo", (ArchitecturalDiagnosticIds.DependencyNotAllowed, 1), (ArchitecturalDiagnosticIds.DependencyRequiredMissing, 1), (ArchitecturalDiagnosticIds.TypeNotAllowed, 1), (ArchitecturalDiagnosticIds.DependencyReverseDirection, 1), (ArchitecturalDiagnosticIds.DependencyPeerScope, 1)),
        Expect("Documentation/Example.VisualStudioSiteDiagnostics"),
        Expect("Features/Example.WildcardTo")
    ];

    private static ExampleBuildExpectation Expect(string projectName, params (string Id, int Count)[] diagnostics)
    {
        var result = new ExampleBuildExpectation(projectName, ExampleConfigStyle.InlineInExample, diagnostics.ToDictionary(diagnostic => diagnostic.Id, diagnostic => diagnostic.Count, StringComparer.Ordinal));

        return result;
    }

    private static ExampleBuildExpectation ExpectFile(string projectName, params (string Id, int Count)[] diagnostics)
    {
        var result = new ExampleBuildExpectation(projectName, ExampleConfigStyle.SettingsFile, diagnostics.ToDictionary(diagnostic => diagnostic.Id, diagnostic => diagnostic.Count, StringComparer.Ordinal));

        return result;
    }
}

internal enum ExampleConfigStyle
{
    InlineInExample,
    SettingsFile
}

internal sealed record ExampleBuildExpectation(string RelativeProjectPath, ExampleConfigStyle ConfigStyle, IReadOnlyDictionary<string, int> Diagnostics);