# ARCH_OPCT_008: operation-contract shape mismatch

`PizzaKitchen.PlacePizzaOrder` is the selected operation owner, but it returns `string` instead of the configured `PlacePizzaOrderResponse`.

```cmd
dotnet build Examples\Diagnostics\OPCT\Example.Arch_OPCT_008.ResponseShapeMismatch -c Release
```
