# Observed dependency cycles

This example shows the difference between a configured cycle and an observed cycle.

- The config intentionally allows `Ordering -> Notifications` and `Notifications -> Ordering`.
- `enforceAcyclic` stays off, so `ARCH007` does not apply.
- The source code uses both directions, so `ARCH018` reports the actual cycle seen in code.

Build in Release to run the analyzer:

```cmd
dotnet build Examples\Diagnostics\Example.Arch018.ObservedCycle -c Release
```
