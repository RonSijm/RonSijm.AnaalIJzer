# ASP.NET Core Model-Binding Names

`DoctorId patientId` and `PatientId doctorId` are valid C#, but convention-based HTTP/model binding can still treat the misleading parameter names as meaningful. The configured `RequireDeclarationNameMatchesType` rule therefore reports two `ARCH_NAME_008` findings.

This rule is intentionally narrower than ASP.NET routing analysis. It knows that a selected action parameter's identifier must agree with its semantic type; it does **not** claim that it compares the `"{patientId}"` route token with the parameter.
