# ARCH_OPCT_002: required owner invocation missing

This example uses an explicit root-level `<Operations>` manifest. It does not guess that a controller, route, queue, or method suffix represents an application operation.

- `PizzaOrderController.PlacePizzaOrder` is a valid entry point because it calls the configured kitchen owner directly.
- `PizzaOrderPreviewController.PlacePizzaOrder` produces `ARCH_OPCT_002` because it returns a response without calling that owner.
- `PizzaKitchen.PlacePizzaOrder` is the valid owner and uses the configured request and response shapes.

The workspace host adds a separate finding when no owner or more than one owner matches across the inspected project or solution. That global cardinality check is intentionally not emitted by a single compiler invocation.

```cmd
dotnet build Examples\Diagnostics\OPCT\Example.Arch_OPCT_002.RequiredOwnerInvocation -c Release
```
