// ReSharper disable All - Justification: Example File

using Microsoft.AspNetCore.Mvc;

namespace Example.AspNetCore.ModelBindingNames;

[ApiController]
[Route("api/patients")]
public sealed class PatientController : ControllerBase
{
    [HttpGet("{patientId}")]
    public GetPatientResponse GetPatient([FromRoute] DoctorId patientId, PatientId doctorId)
    {
        var result = new GetPatientResponse(patientId.Value);

        return result;
    }
}

public interface IHonestType;
public readonly record struct PatientId(int Value) : IHonestType;
public readonly record struct DoctorId(int Value) : IHonestType;
public sealed record GetPatientResponse(int PatientId);
