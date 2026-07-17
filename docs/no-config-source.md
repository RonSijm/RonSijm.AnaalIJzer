## No config source = no diagnostics

If no `Architecture.anl` additional file or `AssemblyMetadata("AnaalIJzerSettings", ...)` value is present, the analyzer is completely silent. This makes the analyzer **opt-in per project**: you can reference it in a shared analyzer package and only activate it in the projects that supply config.

---
