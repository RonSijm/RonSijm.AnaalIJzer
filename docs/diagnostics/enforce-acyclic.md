### `enforceAcyclic` attribute

Set `enforceAcyclic="true"` to require explicit allowed dependency edges to form an acyclic graph. A cycle reports ARCH007 before code needs to use every permitted direction:

```xml
<ArchitecturalLevels enforceAcyclic="true">
  <AllowedDependency from="Ordering" to="Inventory" />
  <AllowedDependency from="Inventory" to="Billing" />
  <AllowedDependency from="Billing" to="Ordering" />
</ArchitecturalLevels>
```

Wildcard and self-edges are excluded because they do not describe a finite directional chain. An unfiltered matching `<BlockedDependency>` removes blocked directions from cycle evaluation.

**Example project:** [`Example.Arch007.CyclicGraph`](../../Examples/Diagnostics/Example.Arch007.CyclicGraph)
