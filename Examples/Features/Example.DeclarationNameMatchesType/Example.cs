// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
<ArchitecturalLevels>
  <Layer name="Endpoints">
    <Class endsWith="Endpoint" />
    <NameRules>
      <RequireDeclarationNameMatchesType allowedSites="Constructor, Method, MethodReturn, Field, Property, Local">
        <Type implements="IHonestType" />
      </RequireDeclarationNameMatchesType>
    </NameRules>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.DeclarationNameMatchesType;

public interface IHonestType;
public readonly record struct PatientId(int Value) : IHonestType;
public readonly record struct DoctorId(int Value) : IHonestType;

// Every declaration below agrees with its semantic type.
public sealed class ValidPatientEndpoint(PatientId patientId)
{
    private readonly PatientId _patientId = patientId;
    public PatientId PatientId { get; } = patientId;

    public void Load(PatientId patientId)
    {
    }

    public PatientId GetPatientId()
    {
        PatientId patientId = default;

        return patientId;
    }
}

// ARCH008 at Constructor: DoctorId does not describe patientId.
public sealed class PatientEndpoint(DoctorId patientId)
{
    // ARCH008 at Field.
    private readonly DoctorId _patientId = patientId;

    // ARCH008 at Property.
    public DoctorId PatientId { get; } = patientId;

    // ARCH008 at Method.
    public void Load(DoctorId patientId)
    {
    }

    // ARCH008 at MethodReturn.
    public DoctorId GetPatientId()
    {
        // ARCH008 at Local.
        DoctorId patientId = default;

        return patientId;
    }
}
