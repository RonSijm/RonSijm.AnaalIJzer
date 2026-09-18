# ARCH_OPER_011: Maximum Operation Count

Every selected pizza preparation may call `ServiceBell.Ring()` once. `PrepareMargheritaPizza` obeys the limit; the second call in `PrepareMysteryPizza` produces one `ARCH_OPER_011`.

```cmd
dotnet build Examples\Diagnostics\OPER\Example.Arch_OPER_011.MaximumOperationCount -c Release
```
