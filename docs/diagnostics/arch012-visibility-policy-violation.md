### ARCH012 - Visibility policy violation

Reported when a source declaration belongs to a layer with an applicable `<VisibilityPolicy>` and its declared accessibility does not pass that policy.

Example:

```text
'SourLollyQueryable' (layer RepositoryQuerySurface) is declared Public:
the VisibilityPolicy for Type in layer 'RepositoryQuerySurface' allows only Internal, File
```

The diagnostic is reported on an accessibility modifier when one exists, otherwise on the declaration identifier.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `DeclarationTarget`
- `DeclaredAccessibility`
- `ViolationReason`
- the originating rule path, line, and column

Typical fixes:

- reduce the declaration's accessibility;
- narrow or change the policy when the public declaration is intentional;
- move the declaration to a layer whose visibility contract matches its responsibility.

**Example project:** [`Example.Arch012.VisibilityPolicy`](../../Examples/Diagnostics/Example.Arch012.VisibilityPolicy)
