# ARCH021: Direct Clock Access

`PizzaKitchen` may use `PizzaClock`, but it may not read `DateTime.UtcNow`, `DateTime.Now`, or `DateTime.Today` directly. The three direct property reads produce `ARCH021`; the injected clock remains allowed.

```cmd
dotnet build Examples\Diagnostics\Example.Arch021.ClockAccess -c Release
```
