# ARCH_OPER_001: Service Location Outside Composition

`IServiceProvider.GetService` is forbidden only for `PizzaKitchen`. The composition root is deliberately outside that layer and may use the provider while it assembles the restaurant.

```cmd
dotnet build Examples\Diagnostics\OPER\Example.Arch_OPER_001.ServiceLocation -c Release
```
