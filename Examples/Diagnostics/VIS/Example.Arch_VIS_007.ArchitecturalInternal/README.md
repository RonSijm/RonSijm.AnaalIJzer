# Architectural-internal annotations

Sometimes a type has to be `public` for reflection, serialization, an ORM, or another framework. That does not necessarily make it part of the architecture's public surface.

This example uses the small `RonSijm.AnaalIJzer.Annotations` package to add three explicit boundaries:

- `[ClassInternal]` keeps a declaration inside its declaring type and nested type family. It does not need a layer configuration.
- `[AssemblyInternal]` keeps a declaration inside its compiled assembly.
- `[LayerInternal]` keeps a declaration inside its deepest configured layer and descendant layers.

The attributes do not change CLR accessibility. They let AnaalIJzer reject ordinary source references and public API leaks that cross the selected architectural boundary.

The project deliberately produces three `ARCH_VIS_007` diagnostics: one for each boundary.
