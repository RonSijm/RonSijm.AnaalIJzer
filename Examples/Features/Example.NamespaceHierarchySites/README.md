# Namespace hierarchy site filters

This project tests every dependency site with a `DescendantToAncestor` namespace hierarchy block. Each `Sites/<Site>/` folder has two files:

- `AllowedSites<Site>Example.cs` uses `allowedSites="<Site>"`. On a `<BlockedRelation>`, this means the block applies only at that named site; a second dependency at a different site stays allowed.
- `BlockedSites<Site>Example.cs` uses `blockedSites="<Site>"`. On a `<BlockedRelation>`, this means the named site is excluded from the block while a second dependency at another site raises `ARCH_NS_007`.

The names describe the XML attribute in use, not a permission grant. `allowedSites` is a filter on the block rule itself. Every source file contains exactly one intended `ARCH_NS_007`, for twenty-six diagnostics in total.

```cmd
dotnet build Examples\Features\Example.NamespaceHierarchySites -c Release
```
