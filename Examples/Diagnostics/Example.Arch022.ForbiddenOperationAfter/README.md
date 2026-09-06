# ARCH022: Forbidden Operation After

`PizzaTicket.Print()` is allowed before an order is committed, but not after `PizzaOrder.Commit()`. The broken preparation prints after committing and produces one `ARCH022`.

```cmd
dotnet build Examples\Diagnostics\Example.Arch022.ForbiddenOperationAfter -c Release
```
