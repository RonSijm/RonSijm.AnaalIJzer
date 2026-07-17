### ARCH007 - Cyclic architecture dependency graph

Reported when `enforceAcyclic="true"` and the explicit allowed dependency graph contains a cycle. The message prints the detected chain, for example `Ordering -> Inventory -> Billing -> Ordering`.

**Example project:** [`Example.Arch007.CyclicGraph`](../../Examples/Diagnostics/Example.Arch007.CyclicGraph)
