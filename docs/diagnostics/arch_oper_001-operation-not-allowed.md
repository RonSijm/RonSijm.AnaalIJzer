### ARCH_OPER_001 - Forbidden operation policy violation

Reported when code in a layer with an applicable `<ForbiddenOperations>` policy uses a selected resolved operation.

Example:

```text
'PizzaKitchen' (layer Kitchen) may not use operation 'System.DateTime.UtcNow' at StaticMember:
the ForbiddenOperations policy in layer 'Kitchen' blocks System.DateTime.UtcNow at StaticMember
```

The diagnostic is reported on the selected source expression. It is symbol-based rather than text-based, so `using Clock = System.DateTime; Clock.UtcNow` and `global::System.DateTime.UtcNow` are the same configured operation.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `Site`
- `OperationKind`
- `OperationDisplayName`
- `OperationPolicyRule`
- `ViolationReason`
- the originating rule path, line, and column

Typical fixes:

- inject or explicitly pass an adapter, such as a restaurant clock, instead of reading a framework static member;
- use the intended asynchronous API instead of blocking with `Task.Wait()` or `Task<T>.Result`;
- move service resolution to the composition root rather than locating a dependency in application code;
- narrow the policy only when that exact operation is intentionally allowed in the owning layer.

There is no automatic code fix. AnaalIjzer can identify the selected forbidden operation, but the correct architectural replacement belongs to the application.

**Focused examples:** [`Example.Arch_OPER_001.ClockAccess`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_001.ClockAccess), [`Example.Arch_OPER_001.BlockingTaskAccess`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_001.BlockingTaskAccess), [`Example.Arch_OPER_001.ServiceLocation`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_001.ServiceLocation), and [`Example.Arch_OPER_001.SelectedEnvironmentMember`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_001.SelectedEnvironmentMember).

#### Real-world uses

- Force application code to receive time through a clock abstraction, keeping business decisions deterministic in tests.
- Prevent blocking `Task.Result`, `Task.Wait()`, or service-location calls from appearing in request-handling code where they hide dependencies or cause scalability problems.
