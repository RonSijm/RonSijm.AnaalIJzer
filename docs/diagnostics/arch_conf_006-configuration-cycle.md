### ARCH_CONF_006 - Cyclic architecture dependency graph

Reported when `enforceAcyclic="true"` and the explicit allowed dependency graph contains a cycle. The message prints the detected chain, for example `Ordering -> Inventory -> Billing -> Ordering`, so the loop does not have to be reconstructed by hand from three rules written on three different days.

**Example project:** [`Example.Arch_CONF_006.CyclicGraph`](../../Examples/Diagnostics/CONF/Example.Arch_CONF_006.CyclicGraph)

#### Real-world uses

- Reject a proposed set of allowed module edges that would let Ordering, Billing, and Inventory depend on one another in a loop.
- Keep a configuration review honest when individually reasonable exceptions accidentally create a cyclic architectural policy as a whole.
