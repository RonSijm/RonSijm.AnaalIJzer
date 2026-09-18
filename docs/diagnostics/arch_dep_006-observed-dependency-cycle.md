## ARCH_DEP_006 - Observed architectural dependency cycle

`ARCH_DEP_006` reports when the dependencies that currently exist in source code form a cycle between configured layers.

Example message:

```text
Observed architectural dependency cycle: Ordering -> Notifications -> Ordering
```

This is intentionally different from `ARCH_CONF_006`:

- `ARCH_CONF_006` says the configuration permits a cycle;
- `ARCH_DEP_006` says the code is currently using a cycle.

Typical causes:

- two architectural areas have started calling each other directly over time;
- one boundary grew a convenience reverse dependency;
- both directions are allowed, but the current code reality has become circular.

Nobody sets out to design a cycle. A cycle is what is left over after several individually reasonable decisions.

Important behavior:

- `ARCH_DEP_006` only appears when `enforceObservedAcyclic="true"` is enabled;
- the cycle is built from observed source dependency sites, not from hypothetical allowed edges;
- direct diagnostics such as `ARCH_DEP_001` and `ARCH_DEP_004` still report separately;
- `arse inspect --solution` can find cross-project observed cycles that one project build cannot see by itself.

See [`Example.Arch_DEP_006.ObservedCycle`](../../Examples/Diagnostics/DEP/Example.Arch_DEP_006.ObservedCycle).

### Real-world uses

- Reveal that an Order module calls Notifications and Notifications now calls Order back, even though both directions were once allowed separately.
- Find a solution-wide cycle introduced by cross-project source dependencies before it turns into a deployment or testing knot.
