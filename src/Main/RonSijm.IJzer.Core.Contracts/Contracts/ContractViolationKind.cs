namespace RonSijm.IJzer.Core.Contracts.Contracts;

public enum ContractViolationKind
{
    DisallowedTypeKind,
    NestedTypeForbidden,
    DisallowedMemberKind,
    StaticMemberForbidden,
    MethodBodyForbidden,
    DisallowedPropertyAccessor
}