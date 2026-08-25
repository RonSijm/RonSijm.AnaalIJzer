// ReSharper disable All - Justification: Example File

using System.Threading.Tasks;

namespace Example.HonestTypeEndpointNames;

public sealed class PatientEndpoint
{
    // Both types are valid endpoint inputs, but external binders commonly use the
    // parameter names. ARCH008 reports both swapped declarations independently.
    public Task<GetPatientResponse> GetPatient(DoctorId patientId, PatientId doctorId)
    {
        return Task.FromResult(new GetPatientResponse());
    }
}
