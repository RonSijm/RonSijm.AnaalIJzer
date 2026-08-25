# Honest Type Endpoint Names

This scenario shows why a strong parameter type does not make a convention-based external binding name correct.

`DoctorId patientId` and `PatientId doctorId` are valid C#, but an HTTP/model-binding layer can still bind values by the misleading parameter names. The layer-scoped `RequireDeclarationNameMatchesType` rule selects types implementing `IHonestType` at the `Method` declaration site and reports both mismatches as ARCH008.

The rule is framework-independent. It does not know about ASP.NET, JSON, routes, doctors, or patients.
