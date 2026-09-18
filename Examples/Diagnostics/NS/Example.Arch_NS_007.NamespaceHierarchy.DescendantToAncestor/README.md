# Descendant-to-ancestor namespace dependency

`Restaurant.Orders` is below `Restaurant`, so an order type cannot depend on a root-level chef when `DescendantToAncestor` is blocked.

- `HeadChef.Review(OrderTicket)` is allowed because it travels from the root down into `Restaurant.Orders`.
- `OrderTicket(HeadChef chef)` raises `ARCH_NS_007` because it travels back up to the root.

```cmd
dotnet build Examples\Diagnostics\NS\Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor -c Release
```
