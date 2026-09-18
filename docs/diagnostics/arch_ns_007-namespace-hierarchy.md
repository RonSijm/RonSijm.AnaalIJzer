## ARCH_NS_007 - Namespace hierarchy dependency violation

`ARCH_NS_007` means a resolved type dependency crossed a relationship blocked by a root-level `<NamespaceHierarchyPolicy>`.

Example:

```text
'OrderTicket' (namespace 'Restaurant.Orders') may not depend on 'HeadChef' (namespace 'Restaurant') at Constructor:
NamespaceHierarchyPolicy 'Restaurant' blocks DescendantToAncestor dependencies.
```

The rule is about source ownership, not runtime request flow. In the restaurant example, an order detail may not reach upward into a root-level chef implementation just because both happen to live beneath `Restaurant`.

The analyzer reports the diagnostic at the actual dependency site and supports constructor and method signatures, fields, properties, locals, object creation, generic arguments and invocations, inheritance, interface implementation, attributes, and static member access. A `using` directive alone does not create `ARCH_NS_007`.

### Typical fixes

- Move the shared abstraction to a namespace both sides are allowed to use.
- Replace the direct reference with a contract owned by the appropriate boundary.
- Adjust the blocked relationship only when the dependency direction is deliberate.
- Scope a blocked relation to selected sites when the ownership rule is intentionally narrower.

`ARCH_NS_007` has no automatic code fix. Moving ownership or choosing a contract is an architectural decision; an automatic change would be guesswork with a very confident-looking diff.

### Diagnostic properties

- `CallerTypeName`
- `DepTypeName`
- `CallerNamespace`
- `DependencyNamespace`
- `NamespaceHierarchyRoot`
- `NamespaceHierarchyRelation`
- `NamespaceHierarchyRuleXmlPath`
- `NamespaceHierarchyRuleXmlLine`
- `NamespaceHierarchyRuleXmlCol`
- `Site`
- `ViolationReason`
- `Comment`

**Focused examples:** [`Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor), [`Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant), [`Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling), and [`Example.Arch_NS_007.NamespaceHierarchy.SameNamespace`](../../Examples/Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SameNamespace).
