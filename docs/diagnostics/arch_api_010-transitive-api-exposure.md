## ARCH_API_010 - Forbidden transitive exposure

ARCH_API_010 reports when an externally visible declaration exposes an allowed root type whose public object graph reaches a type rejected by the owning layer's `<ApiSurface>` policy.

```csharp
public class CandyReceipt
{
    public LollyQueryable RawQuery { get; init; } = new();
}

// ARCH_API_010: CandyOrderingService.OrderRawLolly
//          -> CandyReceipt.RawQuery
//          -> LollyQueryable
public CandyReceipt OrderRawLolly()
{
    return new CandyReceipt();
}
```

The primary location is the root signature, because that declaration publishes the unsafe graph. When the nested member is source-backed, its declaration is included as an additional diagnostic location.

Diagnostic properties include:

- `ApiMemberName` and `ExposureRootMember`;
- `ExposurePath` and `ExposureDepth`;
- `NestedMemberName` and `NestedMemberContainingType`;
- the forbidden type and layer;
- the nested member's canonical `Site`;
- the exact policy reason and configuration origin.

A direct forbidden type reports ARCH_API_001 instead. The two diagnostics are deliberately not duplicated; one complaint per leak is sufficient.

**Example project:** [`Example.Arch_API_010.TransitiveExposure`](../../Examples/Diagnostics/API/Example.Arch_API_010.TransitiveExposure)

### Real-world uses

- Catch a public response DTO that looks harmless at the root but contains an internal query object or persistence entity several properties deeper.
- Prevent a collection, wrapper, or generic result type from reintroducing an API type that the direct public signature correctly avoided.
