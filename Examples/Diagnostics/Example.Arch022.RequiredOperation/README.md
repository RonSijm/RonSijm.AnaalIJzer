# ARCH022: Required Operation

Every `*Pizza` preparation in `PizzaKitchen` must call `PizzaSafetyCheck.Validate()`. `PrepareMargheritaPizza` does; `PrepareMysteryPizza` deliberately does not and produces one `ARCH022`.

```cmd
dotnet build Examples\Diagnostics\Example.Arch022.RequiredOperation -c Release
```
