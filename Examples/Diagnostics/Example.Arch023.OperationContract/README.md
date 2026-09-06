# Operation contract violation

This example uses an explicit root-level `<Operations>` manifest. It does not guess that a controller, route, queue, or method suffix represents an application operation.

- `PizzaOrderController.PlacePizzaOrder` is a valid entry point because it calls the configured kitchen owner directly.
- `PizzaOrderPreviewController.PlacePizzaOrder` produces `ARCH023` because it returns a response without calling that owner.
- `PizzaKitchen.PlacePizzaOrder` produces `ARCH023` because it returns `string` instead of `PlacePizzaOrderResponse`.

The workspace host adds a separate finding when no owner or more than one owner matches across the inspected project or solution. That global cardinality check is intentionally not emitted by a single compiler invocation.

```cmd
dotnet build Examples\Diagnostics\Example.Arch023.OperationContract -c Release
```
