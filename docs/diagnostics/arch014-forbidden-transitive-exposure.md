## ARCH014 - Forbidden transitive exposure

ARCH014 reports when an externally visible declaration exposes an allowed root type whose public object graph reaches a type rejected by the owning layer's `<ApiSurface>` policy.

```csharp
public class CandyReceipt
{
    public LollyQueryable RawQuery { get; init; } = new();
}

// ARCH014: CandyOrderingService.OrderRawLolly
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

A direct forbidden type reports ARCH009 instead. The two diagnostics are deliberately not duplicated.

**Example project:** [`Example.Arch014.TransitiveExposure`](../../Examples/Diagnostics/Example.Arch014.TransitiveExposure)
