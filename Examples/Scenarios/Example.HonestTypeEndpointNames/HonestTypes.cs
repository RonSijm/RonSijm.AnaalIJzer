// ReSharper disable All - Justification: Example File

namespace Example.HonestTypeEndpointNames;

public interface IHonestType;
public readonly record struct PatientId(int Value) : IHonestType;
public readonly record struct DoctorId(int Value) : IHonestType;
public sealed record GetPatientResponse;
