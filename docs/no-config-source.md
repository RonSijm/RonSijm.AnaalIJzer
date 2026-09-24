## No config source = no diagnostics

If no `Architecture.anl` additional file or `AssemblyMetadata("AnaalIJzerSettings", ...)` value is present, the analyzer is completely silent. This makes the analyzer **opt-in per project**: you can reference it centrally and activate it only in projects that supply configuration. Adoption can then happen one project at a time, which is usually the only pace at which adoption happens at all.

---
