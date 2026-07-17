### `enableReport` / `reportPath` attributes

When `enableReport="true"` is set on `<ArchitecturalLevels>`, Arse uses `reportPath` as the default output for `arse report`. The path is resolved relative to the config file; for inline `AssemblyMetadata("AnaalIJzerSettings", ...)`, it is resolved relative to the project file. If omitted, Arse defaults to `architectural-violations.md` next to the project. Solution-level reports use the first configured project as the representative settings source; if no `reportPath` is enabled there, Arse writes `architectural-violations.md` next to the solution.

```xml
<ArchitecturalLevels enableReport="true"
                     reportPath="../../docs/architectural-violations.md">
  …
</ArchitecturalLevels>
```
