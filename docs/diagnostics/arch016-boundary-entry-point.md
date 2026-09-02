## ARCH016 - Boundary entry-point violation

`ARCH016` reports when a dependency already passed the normal dependency graph, but still enters a boundary through the wrong child layer or type.

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

- if the dependency is already illegal for the usual reasons, you still get `ARCH001`, `ARCH003`, `ARCH004`, or `ARCH005`;
- `ARCH016` only appears when the dependency graph allowed the dependency first.

See [`Example.Arch016.BoundaryEntryPoints`](../../Examples/Diagnostics/Example.Arch016.BoundaryEntryPoints).
