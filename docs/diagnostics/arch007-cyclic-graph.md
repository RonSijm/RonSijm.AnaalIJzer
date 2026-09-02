### ARCH007 - Cyclic architecture dependency graph

Reported when `enforceAcyclic="true"` and the explicit allowed dependency graph contains a cycle. The message prints the detected chain, for example `Ordering -> Inventory -> Billing -> Ordering`, so the loop does not have to be reconstructed by hand from three rules written on three different days.

**Example project:** [`Example.Arch007.CyclicGraph`](../../Examples/Diagnostics/Example.Arch007.CyclicGraph)
