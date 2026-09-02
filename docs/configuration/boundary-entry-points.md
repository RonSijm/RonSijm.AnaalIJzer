## Boundary entry points

`<EntryPoints>` lets a parent boundary say which child layers or specific dependency types are valid external doors into that boundary.

Restaurant version:

- the kitchen may have several rooms inside it;
- outside staff may enter through the service counter;
- they should not walk straight into the cooking area.

That is different from `<AllowedDependency>`:

- `<AllowedDependency>` says whether one role may depend on another at all;
- `<EntryPoints>` says which part of a boundary is the allowed doorway after the dependency is otherwise legal.

Example:

```xml
<Layer name="Ordering">
  <Namespace startsWith="Shop.Ordering" />

  <EntryPoints>
    <EntryPoint layer="Contracts" />
    <EntryPoint allowedSites="Method">
      <Class endsWith="OrderingFacade" />
    </EntryPoint>
  </EntryPoints>

  <Layer name="Contracts">
    <Class typeKind="Interface" />
  </Layer>

  <Layer name="Implementation">
    <Class typeKind="Class" />
  </Layer>
</Layer>
```

Rules:

- no `<EntryPoints>` means no `ARCH016`;
- entry points only restrict callers outside the owning boundary;
- internal calls inside the same boundary are unchanged;
- nested boundaries are cumulative from outermost to innermost;
- entry points never grant a dependency that `<AllowedDependency>` would deny.

A door is only meaningful in a wall that already exists.

### Selector forms

Each `<EntryPoint>` uses exactly one selector form:

| Form | Meaning |
|---|---|
| `layer="Contracts"` | permit entry through that descendant layer subtree |
| matcher elements | permit entry through dependency types matching `<Class>`, `<Namespace>`, or `<Assembly>` |

`allowedSites` and `blockedSites` work the same way as on dependency edges.

### Example

See [`Example.Arch016.BoundaryEntryPoints`](../../Examples/Diagnostics/Example.Arch016.BoundaryEntryPoints), where `Presentation -> Ordering` is allowed in general, but only `Ordering/Contracts` is a valid external entry point.
