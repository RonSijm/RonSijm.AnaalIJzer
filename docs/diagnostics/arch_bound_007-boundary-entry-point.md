## ARCH_BOUND_007 - Boundary entry-point violation

`ARCH_BOUND_007` reports when a dependency already passed the normal dependency graph, but still enters a boundary through the wrong child layer or type.

Example message:

```text
'CandyController' (layer Presentation) may not enter boundary 'Ordering' through 'CandyOrderingService' (layer Ordering/Implementation): the boundary permits entry only through Ordering/Contracts
```

Typical causes:

- a controller reaches into an implementation layer instead of a contract layer;
- a facade entry point is allowed only at certain sites, but the dependency appears at a blocked site;
- nested boundaries define progressively narrower external entry doors.

#### IDE code fixes

The IDE can add a missing `<EntryPoint>`, add the current site to an entry point's `allowedSites`, or remove the current site from `blockedSites` when the boundary policy is too narrow for the intended call shape.

Important precedence rule:

- if the dependency is already illegal for the usual reasons, you still get `ARCH_DEP_001`, `ARCH_TYPE_001`, `ARCH_DEP_004`, or `ARCH_DEP_005`;
- `ARCH_BOUND_007` only appears when the dependency graph allowed the dependency first.

In restaurant terms: you are welcome in the building, just not through the kitchen window.

See [`Example.Arch_BOUND_007.BoundaryEntryPoints`](../../Examples/Diagnostics/BOUND/Example.Arch_BOUND_007.BoundaryEntryPoints).

### Real-world uses

- Require controllers, jobs, and message consumers to enter an application boundary through its contract or facade layer rather than its implementation classes.
- Keep plug-in or module consumers on a deliberately small public entry surface even when implementation types are otherwise dependency-legal.
