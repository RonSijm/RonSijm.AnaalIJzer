## ARCH018 - Observed architectural dependency cycle

`ARCH018` reports when the dependencies that currently exist in source code form a cycle between configured layers.

Example message:

```text
Observed architectural dependency cycle: Ordering -> Notifications -> Ordering
```

This is intentionally different from `ARCH007`:

- `ARCH007` says the configuration permits a cycle;
- `ARCH018` says the code is currently using a cycle.

Typical causes:

- two architectural areas have started calling each other directly over time;
- one boundary grew a convenience reverse dependency;
- both directions are allowed, but the current code reality has become circular.

Important behavior:

- `ARCH018` only appears when `enforceObservedAcyclic="true"` is enabled;
- the cycle is built from observed source dependency sites, not from hypothetical allowed edges;
- direct diagnostics such as `ARCH001` and `ARCH004` still report separately;
- `arse inspect --solution` can find cross-project observed cycles that one project build cannot see by itself.

See [`Example.Arch018.ObservedCycle`](../../Examples/Diagnostics/Example.Arch018.ObservedCycle).
