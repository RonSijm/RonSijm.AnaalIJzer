namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

public enum SemanticOperationKind
{
	Invocation,
	PropertyRead,
	PropertyWrite,
	FieldRead,
	FieldWrite,
	EventAccess,
	ObjectCreation,
	Conversion,
	Assignment,
	Return,
	Argument
}
