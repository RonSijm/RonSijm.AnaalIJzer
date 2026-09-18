# ARCH_OPER_002: Required Operation Missing

Every `*Pizza` preparation in `PizzaKitchen` must call `PizzaSafetyCheck.Validate()`. `PrepareMargheritaPizza` does; `PrepareMysteryPizza` deliberately does not and produces one `ARCH_OPER_002`.

```cmd
dotnet build Examples\Diagnostics\OPER\Example.Arch_OPER_002.RequiredOperation -c Release
```
