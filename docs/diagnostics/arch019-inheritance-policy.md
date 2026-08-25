### ARCH019 - Inheritance policy violation

Reported when a source declaration belongs to a layer with an applicable `<InheritancePolicy>` and its declared base-type or interface contract does not pass that policy.

Example:

```text
'SyrupEntity' (layer PersistenceEntities) violates inheritance policy at MissingRequiredBaseType:
the InheritancePolicy in layer 'PersistenceEntities' requires one of base types Entity
```

The diagnostic is reported on the declaration identifier.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `InheritanceViolationKind`
- `ViolationReason`
- the originating rule path, line, and column

Typical fixes:

- inherit the required base type;
- implement the missing interface contract;
- move the declaration out of the layer if it is not meant to follow that shared inheritance rule;
- narrow or broaden the policy only when the declaration is intentionally outside the current contract.

**Example project:** [`Example.Arch019.InheritancePolicy`](../../Examples/Diagnostics/Example.Arch019.InheritancePolicy)
