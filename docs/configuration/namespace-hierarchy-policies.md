### Namespace hierarchy policies

`<NamespaceHierarchyPolicy>` protects ownership implied by a namespace tree. It is a root-level policy, not a layer: it works whether or not either type belongs to a `<Layer>`, and it does not add nodes or edges to the layer dependency graph.

The restaurant version is simple: `Restaurant.Orders` owns its order details. A type in that namespace should not reach back up and grab a root-level chef implementation unless the policy deliberately permits it.

```xml
<ArchitecturalLevels>
  <NamespaceHierarchyPolicy rootNamespace="Restaurant"
                            description="Feature namespaces own their implementation details.">
    <BlockedRelation relation="DescendantToAncestor" />
  </NamespaceHierarchyPolicy>
</ArchitecturalLevels>
```

With that configuration, `Restaurant.Orders.OrderTicket` may depend on another type under `Restaurant.Orders`, but it cannot reference `Restaurant.HeadChef` directly. The policy checks semantic references, not `using` directives by themselves.

### Relationships

`rootNamespace` is compared as dot-separated namespace segments. `Restaurant.Orders` is below `Restaurant`; `Restaurant.OrdersArchive` is not. Both the caller and dependency must be inside the configured root.

| `relation` | Caller to dependency | Restaurant reading |
|---|---|---|
| `DescendantToAncestor` | `Restaurant.Orders` -> `Restaurant` | An order detail reaches back up to a root-level chef. |
| `AncestorToDescendant` | `Restaurant` -> `Restaurant.Orders` | A root-level chef reaches down into order details. |
| `SiblingToSibling` | `Restaurant.Orders` -> `Restaurant.Menu` | One feature namespace reaches sideways into another. |
| `SameNamespace` | `Restaurant.Orders` -> `Restaurant.Orders` | Two types in the same namespace reference each other. |

Add one or more `<BlockedRelation>` children. Rules are read in XML order: the first rule that matches both the relationship and the site explains the block.

```xml
<NamespaceHierarchyPolicy rootNamespace="Restaurant">
  <BlockedRelation relation="DescendantToAncestor" />
  <BlockedRelation relation="SiblingToSibling"
                   description="Feature kitchens share a contract, not each other&#39;s internals." />
</NamespaceHierarchyPolicy>
```

This policy has no implicit exception for a parent namespace. If `Restaurant.Orders` needs a root-level shared contract, place that contract in an intentional namespace and choose the relationship rules accordingly.

### Scope a block to dependency sites

`allowedSites` and `blockedSites` are filters on a **blocked relation**. They are not permission grants and they are not the same as `<AllowedDependency allowedSites="...">`.

```xml
<NamespaceHierarchyPolicy rootNamespace="Restaurant">
  <!-- Block only constructor parameters that reach from a child to its parent. -->
  <BlockedRelation relation="DescendantToAncestor" allowedSites="Constructor" />

  <!-- Block the same relationship everywhere except fields. -->
  <BlockedRelation relation="SiblingToSibling" blockedSites="Field" />
</NamespaceHierarchyPolicy>
```

- `allowedSites="Constructor"` means this **block** applies at constructors and nowhere else.
- `blockedSites="Field"` means this **block** applies everywhere except fields.
- The attributes are mutually exclusive.

All architectural dependency sites are supported: `Constructor`, `Method`, `MethodReturn`, `Field`, `Property`, `Local`, `New`, `GenericInvocation`, `GenericArgument`, `Inheritance`, `InterfaceImplementation`, `Attribute`, and `StaticMember`.

### Interaction with layers

Namespace hierarchy policies run before layer dependency rules. When they block a reference, AnaalIJzer reports `ARCH_NS_007` instead of also reporting an `ARCH_DEP_001`, `ARCH_DEP_004`, or `ARCH_DEP_005` for that same reference. When no hierarchy rule blocks it, ordinary layer analysis continues unchanged.

That separation is intentional:

- layers express architectural roles and permitted role-to-role dependencies;
- namespace hierarchy policies express source ownership inside a namespace tree.

Neither mechanism overrides the other. A namespace rule can stop a dependency early; a layer rule can still reject a dependency that the namespace policy leaves alone.

There is no automatic code fix for `ARCH_NS_007`. The analyzer can identify the forbidden direction, but only the application can decide whether to move a type, introduce a contract, or change the ownership boundary.

**Focused examples:**

- [`Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor)
- [`Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant)
- [`Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling)
- [`Example.Arch_NS_007.NamespaceHierarchy.SameNamespace`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SameNamespace)
- [`Example.NamespaceHierarchySites`](../../Examples/Features/Example.NamespaceHierarchySites) - every dependency site with both site-filter forms.
