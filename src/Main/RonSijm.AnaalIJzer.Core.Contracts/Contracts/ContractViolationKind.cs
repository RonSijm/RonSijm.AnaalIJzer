namespace RonSijm.AnaalIJzer.Contracts;

public enum ContractViolationKind
{
	DisallowedTypeKind,
	NestedTypeForbidden,
	DisallowedMemberKind,
	StaticMemberForbidden,
	MethodBodyForbidden,
	DisallowedPropertyAccessor
}
