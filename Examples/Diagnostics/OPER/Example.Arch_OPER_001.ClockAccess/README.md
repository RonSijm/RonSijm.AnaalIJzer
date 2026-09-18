# ARCH_OPER_001: Direct Clock Access

`PizzaKitchen` may use `PizzaClock`, but it may not read `DateTime.UtcNow`, `DateTime.Now`, or `DateTime.Today` directly. The three direct property reads produce `ARCH_OPER_001`; the injected clock remains allowed.

```cmd
dotnet build Examples\Diagnostics\OPER\Example.Arch_OPER_001.ClockAccess -c Release
```
