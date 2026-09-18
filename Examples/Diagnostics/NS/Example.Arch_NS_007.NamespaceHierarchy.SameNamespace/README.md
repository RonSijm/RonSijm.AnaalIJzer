# Same-namespace dependency

`SameNamespace` is useful for a deliberately strict feature namespace: its types must not quietly turn into a web of direct references.

- `OrderTicket(PizzaMenu)` raises `ARCH_NS_007` because both types belong to `Restaurant.Orders`.
- `HeadChef(OrderTicket)` is allowed because the caller belongs to the `Restaurant` root namespace.

```cmd
dotnet build Examples\Diagnostics\NS\Example.Arch_NS_007.NamespaceHierarchy.SameNamespace -c Release
```
