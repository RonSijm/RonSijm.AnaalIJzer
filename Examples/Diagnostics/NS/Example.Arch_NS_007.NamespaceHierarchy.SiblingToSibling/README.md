# Sibling-to-sibling namespace dependency

`Restaurant.Orders` and `Restaurant.Payments` are separate children of `Restaurant`. Blocking `SiblingToSibling` keeps those features from taking direct dependencies on each other.

- `OrderSupervisor(HeadChef)` is allowed because `Restaurant` is an ancestor, not a sibling.
- `OrderTicket(PaymentLedger)` raises `ARCH_NS_007` because Orders reaches directly into Payments.

```cmd
dotnet build Examples\Diagnostics\NS\Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling -c Release
```
