namespace RonSijm.AnaalIJzer.Core.OperationContracts.Model;

public enum OperationContractViolationKind
{
	OwnerOutsideAllowedLayer,
	OwnerMissingRequest,
	OwnerInvalidResponse,
	EntryPointOutsideAllowedLayer,
	EntryPointMissingRequest,
	EntryPointInvalidResponse,
	EntryPointDoesNotInvokeOwner
}
