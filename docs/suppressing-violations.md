## Suppressing a violation

If you have a justified exception to the rule, suppress it with a standard `#pragma` using the specific ID for the reason you want to allow (`ARCH001`, `ARCH004` or `ARCH005`):

```csharp
#pragma warning disable ARCH001 // justified: bootstrapping cross-cutting concern
public class DiagnosticsController(IHealthRepository health) : ControllerBase { }
#pragma warning restore ARCH001
```

Or use a `[SuppressMessage]` attribute on the class:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Architecture", "ARCH001",
    Justification = "Bootstrapping concern that intentionally crosses layers")]
public class DiagnosticsController(IHealthRepository health) : ControllerBase { }
```

To silence one *category* across an entire project without touching individual files, add the ID to `<NoWarn>` in the `.csproj` - for example `<NoWarn>$(NoWarn);ARCH005</NoWarn>` to allow same-layer dependencies while keeping ARCH001 and ARCH004 as errors.

---
