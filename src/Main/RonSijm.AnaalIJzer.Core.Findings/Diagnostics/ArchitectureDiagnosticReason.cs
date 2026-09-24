namespace RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

public enum ArchitectureDiagnosticReason
{
    NotAllowed = 1,
    RequiredMissing = 2,
    InvalidDefinition = 3,
    ReverseDirection = 4,
    PeerScope = 5,
    Cycle = 6,
    BoundaryPlacement = 7,
    ShapeMismatch = 8,
    ReviewLifecycle = 9,
    TransitiveReachability = 10,
    Cardinality = 11,
    Ordering = 12
}