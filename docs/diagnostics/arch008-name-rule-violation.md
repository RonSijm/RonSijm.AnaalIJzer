### ARCH008 - Name rule violation

Reported when a value movement or declaration inside a layer matches an applicable `<NameRules>` policy, but the compared names do not normalize to the same meaning and no matching `<Allow>` mapping permits that site. Most findings turn out to be a misleading name rather than wrong behaviour, which is precisely the point: the name is what the next reader believes.

Example message:

```text
'OrderService' (layer Application) violates name rule 'RequireMatchingNames' at Property:
source 'animalId' normalizes to 'animal.id', target 'Customer.Id' normalizes to 'customer.id'
```

Declaration/type example:

```text
'PatientEndpoint' (layer AspEndpoints) violates name rule
'RequireDeclarationNameMatchesType' at Method: type 'DoctorId' normalizes to
'doctor.id', declaration name 'patientId' normalizes to 'patient.id'
```

**Examples:** [`Example.NameRules`](../../Examples/Features/Example.NameRules), [`Example.DeclarationNameMatchesType`](../../Examples/Features/Example.DeclarationNameMatchesType), and [`Example.HonestTypeEndpointNames`](../../Examples/Scenarios/Example.HonestTypeEndpointNames).

Typical fixes:

- Pass or assign the value with the matching meaning.
- Rename the local, parameter, field, or property when the code is correct but the name is misleading.
- Add a narrow `<Allow>` mapping when the translation is intentional.
- Scope that mapping with `allowedSites` or `blockedSites` when it should only be valid in one kind of code location.
