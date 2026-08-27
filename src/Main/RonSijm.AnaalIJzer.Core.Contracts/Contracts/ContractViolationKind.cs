namespace RonSijm.AnaalIJzer.Core.Contracts.Contracts;

public enum ContractViolationKind
{
	DisallowedTypeKind,
	NestedTypeForbidden,
	DisallowedMemberKind,
	StaticMemberForbidden,
	MethodBodyForbidden,
	DisallowedPropertyAccessor
}
