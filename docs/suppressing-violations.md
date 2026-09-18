## Suppressing a violation

If you have a justified exception to the rule, suppress it with a standard `#pragma` using the specific ID for the reason you want to allow (`ARCH_DEP_001`, `ARCH_DEP_004` or `ARCH_DEP_005`):

```csharp
#pragma warning disable ARCH_DEP_001 // justified: bootstrapping cross-cutting concern
public class DiagnosticsController(IHealthRepository health) : ControllerBase { }
#pragma warning restore ARCH_DEP_001
```

Or use a `[SuppressMessage]` attribute on the class:

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Architecture", "ARCH_DEP_001",
    Justification = "Bootstrapping concern that intentionally crosses layers")]
public class DiagnosticsController(IHealthRepository health) : ControllerBase { }
```

To silence one *category* across an entire project without touching individual files, add the ID to `<NoWarn>` in the `.csproj` - for example `<NoWarn>$(NoWarn);ARCH_DEP_005</NoWarn>` to allow same-layer dependencies while keeping ARCH_DEP_001 and ARCH_DEP_004 as errors.

Write the justification either way. A suppression with a reason is a documented decision; a bare `#pragma` is a puzzle left for whoever opens the file next year.

---
