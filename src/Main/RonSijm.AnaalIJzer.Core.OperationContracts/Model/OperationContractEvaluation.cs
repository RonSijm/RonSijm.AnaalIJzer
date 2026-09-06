namespace RonSijm.AnaalIJzer.Core.OperationContracts.Model;

public readonly struct OperationContractEvaluation(
	OperationContractDefinition definition,
	OperationContractParticipantRole participantRole,
	OperationContractViolationKind violationKind,
	string reason)
{
	public OperationContractDefinition Definition { get; } = definition;

	public OperationContractParticipantRole ParticipantRole { get; } = participantRole;

	public OperationContractViolationKind ViolationKind { get; } = violationKind;

	public string Reason { get; } = reason;
}
