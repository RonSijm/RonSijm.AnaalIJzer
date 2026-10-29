### ARCH_VIS_007 - Architectural-internal boundary violation

Reported when code references or publicly exposes a declaration marked with an IJzer architectural-internal annotation from outside its selected boundary.

The available boundaries are:

- `[ClassInternal]`: the annotated declaration's owning type and nested type family;
- `[AssemblyInternal]`: the declaring assembly;
- `[LayerInternal]`: the declaration's deepest configured layer and descendant layers.

Example:

```text
'OrderingService' may not reference architectural-internal 'EntityProjection':
[LayerInternal] permits references only from layer 'Persistence' or its descendants;
caller layer is 'Application'
```

Diagnostic properties include:

- `CallerTypeName` and `CallerLayerName`;
- `DependencyTypeName` and `DependencyLayerName`;
- `Site`;
- `ArchitecturalInternalScope`;
- `ArchitecturalInternalAttribute`;
- `ViolationReason`.

Typical fixes:

- replace the restricted type with a public contract or projection;
- move the caller inside the intended boundary when it genuinely belongs there;
- choose a wider annotation when the original boundary was too narrow;
- use ordinary `internal` or `private` accessibility when no framework requires the declaration to stay public.

No automatic code fix is offered because those choices change ownership or API design.

**Example project:** [`Example.Arch_VIS_007.ArchitecturalInternal`](../../Examples/Diagnostics/VIS/Example.Arch_VIS_007.ArchitecturalInternal)
