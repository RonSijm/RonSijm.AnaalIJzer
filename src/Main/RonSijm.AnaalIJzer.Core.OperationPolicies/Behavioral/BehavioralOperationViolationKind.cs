namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;

public enum BehavioralOperationViolationKind
{
    MissingRequiredOperation,
    RequiredOperationDoesNotDominateExit,
    MissingRequiredOperationBefore,
    ForbiddenOperationAfter,
    MaximumOperationCountExceeded
}