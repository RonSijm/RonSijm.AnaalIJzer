# ARCH_RET_001: Only Identifier Return

`PizzaKitchen` uses an `<AllowedReturn>` shape allow-list with `<Identifier />`. A direct `return oven.BakePizza();` reports `ARCH_RET_001`; storing the pizza in `result` and returning `result` is allowed.

This is a direct return-shape rule. `Identifier` means a bare named expression, not a proof that the name is specifically a local variable. It can also match a parameter, an unqualified field or property, or a constant.

```cmd
dotnet build Examples\Diagnostics\RET\Example.Arch_RET_001.OnlyIdentifierReturn -c Release
```
