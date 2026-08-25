## Observed dependency cycles

`enforceObservedAcyclic="true"` tells AnaalIJzer to inspect the dependencies that actually occur in source code and fail when those observed edges form a cycle.

This is different from `enforceAcyclic`:

- `enforceAcyclic` checks the configured `<AllowedDependency>` graph;
- `enforceObservedAcyclic` checks the dependencies the code is currently using.

Example:

```xml
<ArchitecturalLevels enforceObservedAcyclic="true">
  <Layer name="Ordering">
    <Namespace startsWith="Shop.Ordering" />
  </Layer>

  <Layer name="Notifications">
    <Namespace startsWith="Shop.Notifications" />
  </Layer>

  <AllowedDependency from="Ordering" to="Notifications" />
  <AllowedDependency from="Notifications" to="Ordering" />
</ArchitecturalLevels>
```

That configuration is still legal as a configured graph. It only becomes `ARCH018` when code really uses both directions and closes the cycle.

Restaurant version:

- the restaurant manual may allow the waiter and the chef to talk both ways;
- `enforceObservedAcyclic` asks whether the current staff behavior has actually turned that into a loop where each role now waits on the next.

Behavior:

- default is `false`;
- accepted values are `true`, `false`, `1`, and `0`;
- invalid values report `ARCH006` and disable observed-cycle enforcement;
- a project build only sees cycles inside that compilation;
- `arse inspect --solution` can also find cross-project observed cycles.

See [`Example.Arch018.ObservedCycle`](../../Examples/Diagnostics/Example.Arch018.ObservedCycle).
