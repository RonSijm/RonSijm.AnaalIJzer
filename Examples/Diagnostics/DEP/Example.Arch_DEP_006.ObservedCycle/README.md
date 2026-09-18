# Observed dependency cycles

This example shows the difference between a configured cycle and an observed cycle.

- The config intentionally allows `Ordering -> Notifications` and `Notifications -> Ordering`.
- `enforceAcyclic` stays off, so `ARCH_CONF_006` does not apply.
- The source code uses both directions, so `ARCH_DEP_006` reports the actual cycle seen in code.

Build in Release to run the analyzer:

```cmd
dotnet build Examples\Diagnostics\DEP\Example.Arch_DEP_006.ObservedCycle -c Release
```
