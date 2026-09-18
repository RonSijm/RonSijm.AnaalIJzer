# ARCH_OPCT_001: operation-contract participant not allowed

`PizzaKitchen.PlacePizzaOrder` matches the configured owner selector, but its `Endpoint` layer is not listed in `allowedOwnerLayers`.

```cmd
dotnet build Examples\Diagnostics\OPCT\Example.Arch_OPCT_001.ParticipantNotAllowed -c Release
```
