# Nested layer boundaries

This example treats each top-level layer as a module boundary with locally named inner layers.

- `Ordering/Application -> Ordering/Repository` passes the internal Ordering gate.
- `Ordering/Application -> Billing/Contracts` passes the root, egress, and ingress gates.
- `OrderModule` belongs directly to `Ordering` because it matches the parent namespace but no child.
- `CatalogService`, `FulfillmentService`, and `InventoryService` each omit a different gate and produce one `ARCH001`.

Build in Release to run the analyzer:

```cmd
dotnet build Examples\Features\Example.NestedLayers -c Release
```
