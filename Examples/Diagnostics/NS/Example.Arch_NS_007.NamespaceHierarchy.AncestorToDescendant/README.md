# Ancestor-to-descendant namespace dependency

`Restaurant` is above `Restaurant.Orders`, so a root-level type cannot directly depend on an order feature type when `AncestorToDescendant` is blocked.

- `HeadChef(OrderTicket)` raises `ARCH_NS_007` because it reaches down into `Restaurant.Orders`.
- `OrderTicket(HeadChef)` is allowed because this rule only blocks the opposite direction.

```cmd
dotnet build Examples\Diagnostics\NS\Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant -c Release
```
