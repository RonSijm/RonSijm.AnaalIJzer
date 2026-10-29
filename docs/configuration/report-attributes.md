### `enableReport` / `reportPath` attributes

When `enableReport="true"` is set on `<ArchitecturalLevels>`, IJzer Forge uses `reportPath` as the default output for `ijzer report`. Fixing the path in configuration keeps CI and local runs writing to the same file, instead of two reports disagreeing with each other from different folders. The path is resolved relative to the config file; for inline `AssemblyMetadata("IJzerSettings", ...)`, it is resolved relative to the project file. If omitted, IJzer Forge defaults to `architectural-violations.md` next to the project. Solution-level reports use the first configured project as the representative settings source; if no `reportPath` is enabled there, IJzer Forge writes `architectural-violations.md` next to the solution.

```xml
<ArchitecturalLevels enableReport="true"
                     reportPath="../../docs/architectural-violations.md">
  …
</ArchitecturalLevels>
```
