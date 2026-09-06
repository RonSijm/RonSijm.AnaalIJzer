# ARCH022: Required Operation Before

`PizzaSafetyCheck.Validate()` must occur before `PizzaOven.Bake()` in every selected pizza preparation. The valid method validates first; the intentionally broken method bakes immediately and produces one `ARCH022` on the baking call.

```cmd
dotnet build Examples\Diagnostics\Example.Arch022.RequiredOperationBefore -c Release
```
