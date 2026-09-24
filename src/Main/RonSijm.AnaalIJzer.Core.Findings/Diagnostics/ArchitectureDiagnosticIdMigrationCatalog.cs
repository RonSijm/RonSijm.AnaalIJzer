namespace RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

public static class ArchitectureDiagnosticIdMigrationCatalog
{
    public static IReadOnlyList<ArchitectureDiagnosticIdMigration> All { get; } =
    [
        OneToOne("ARCH001", ArchitecturalDiagnosticIds.DependencyNotAllowed),
        OneToOne("ARCH002", ArchitecturalDiagnosticIds.DependencyRequiredMissing),
        OneToOne("ARCH003", ArchitecturalDiagnosticIds.TypeNotAllowed),
        OneToOne("ARCH004", ArchitecturalDiagnosticIds.DependencyReverseDirection),
        OneToOne("ARCH005", ArchitecturalDiagnosticIds.DependencyPeerScope),
        OneToOne("ARCH006", ArchitecturalDiagnosticIds.ConfigurationInvalid),
        OneToOne("ARCH007", ArchitecturalDiagnosticIds.ConfigurationCycle),
        OneToOne("ARCH008", ArchitecturalDiagnosticIds.NameShapeMismatch),
        OneToOne("ARCH009", ArchitecturalDiagnosticIds.ApiExposureNotAllowed),
        OneToOne("ARCH010", ArchitecturalDiagnosticIds.ProjectReferenceNotAllowed),
        OneToOne("ARCH011", ArchitecturalDiagnosticIds.PackageReferenceNotAllowed),
        OneToOne("ARCH012", ArchitecturalDiagnosticIds.VisibilityNotAllowed),
        OneToOne("ARCH013", ArchitecturalDiagnosticIds.ContractShapeMismatch),
        OneToOne("ARCH014", ArchitecturalDiagnosticIds.ApiTransitiveExposure),
        OneToOne("ARCH015", ArchitecturalDiagnosticIds.SourceBoundaryPlacement),
        OneToOne("ARCH016", ArchitecturalDiagnosticIds.BoundaryEntryPlacement),
        OneToOne("ARCH017", ArchitecturalDiagnosticIds.ExceptionReviewLifecycle),
        OneToOne("ARCH018", ArchitecturalDiagnosticIds.DependencyCycle),
        OneToOne("ARCH019", ArchitecturalDiagnosticIds.InheritanceNotAllowed),
        OneToOne("ARCH020", ArchitecturalDiagnosticIds.ReturnNotAllowed),
        OneToOne("ARCH021", ArchitecturalDiagnosticIds.OperationNotAllowed),
        new ArchitectureDiagnosticIdMigration(
            "ARCH022",
            [ArchitecturalDiagnosticIds.OperationRequiredMissing, ArchitecturalDiagnosticIds.OperationCardinality, ArchitecturalDiagnosticIds.OperationOrdering],
            "Choose the ID that matches the behavioral-operation failure: missing, count, or ordering."),
        new ArchitectureDiagnosticIdMigration(
            "ARCH023",
            [ArchitecturalDiagnosticIds.OperationContractNotAllowed, ArchitecturalDiagnosticIds.OperationContractRequiredMissing, ArchitecturalDiagnosticIds.OperationContractShapeMismatch],
            "Choose the ID that matches the operation-contract failure: participant, missing requirement, or shape."),
        OneToOne("ARCH024", ArchitecturalDiagnosticIds.AssemblyAttributeNotAllowed),
        OneToOne("ARCH025", ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement),
        OneToOne("TOPO001", ArchitecturalDiagnosticIds.SolutionReferenceNotAllowed),
        OneToOne("TOPO002", ArchitecturalDiagnosticIds.SolutionCycle)
    ];

    public static bool TryGet(string oldId, out ArchitectureDiagnosticIdMigration? migration)
    {
        migration = All.FirstOrDefault(candidate => string.Equals(candidate.OldId, oldId, StringComparison.Ordinal));
        var result = migration is not null;

        return result;
    }

    private static ArchitectureDiagnosticIdMigration OneToOne(string oldId, string newId)
    {
        var result = new ArchitectureDiagnosticIdMigration(oldId, [newId], $"Replace {oldId} with {newId}.");

        return result;
    }
}