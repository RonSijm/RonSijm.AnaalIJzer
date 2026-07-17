## Design note: why generated files live in the tools

The analyzer reports `ARCH00X` diagnostics and deliberately does not write files during compilation. Roslyn analyzers run in IDEs, build servers and design-time builds, so keeping them free of filesystem side effects avoids surprising writes and keeps them closer to Roslyn's analyzer guidance.

The shared tooling engine is the explicit generation host used by both Arse modes. It can load a project with `MSBuildWorkspace` or read an XML settings file directly for documentation. For project-backed operations it reads the same `Architecture.anl` / `AssemblyMetadata("AnaalIJzerSettings", ...)` config as the analyzer and runs the analyzer in-process when a violation report is needed:

- `generate-config` inspects a project and writes a validated baseline configuration.
- `export-config` persists compiled inline `AnaalIJzerSettings` XML.
- `documentation` renders dependency diagrams and rule descriptions with `ArchitectureDocumentationGenerator`.
- `report` runs the analyzer and renders diagnostics with `ArchitecturalViolationReporter`.
- `merge-config` flattens XML files and transitive includes into one configuration.
- `split-config` extracts disconnected dependency graphs into an include-based configuration.

That keeps normal builds focused on diagnostics while still making reports and documentation easy to regenerate in CI or before committing documentation updates.
