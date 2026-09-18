# ARCH_OPER_012: Forbidden Operation After

`PizzaTicket.Print()` is allowed before an order is committed, but not after `PizzaOrder.Commit()`. The broken preparation prints after committing and produces one `ARCH_OPER_012`.

```cmd
dotnet build Examples\Diagnostics\OPER\Example.Arch_OPER_012.ForbiddenOperationAfter -c Release
```
