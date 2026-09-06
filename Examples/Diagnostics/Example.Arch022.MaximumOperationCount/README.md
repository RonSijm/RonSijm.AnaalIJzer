# ARCH022: Maximum Operation Count

Every selected pizza preparation may call `ServiceBell.Ring()` once. `PrepareMargheritaPizza` obeys the limit; the second call in `PrepareMysteryPizza` produces one `ARCH022`.

```cmd
dotnet build Examples\Diagnostics\Example.Arch022.MaximumOperationCount -c Release
```
