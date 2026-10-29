# RonSijm.IJzer.Annotations

Some frameworks need a declaration to remain CLR-public even though application code should treat it as an implementation detail. This package supplies dependency-free markers for that case:

- `[ClassInternal]` keeps a declaration inside its declaring type and nested type family.
- `[AssemblyInternal]` keeps a declaration inside its declaring assembly.
- `[LayerInternal]` keeps a declaration inside its configured IJzer layer and descendant layers.

The attributes do not change CLR accessibility. The `RonSijm.IJzer` analyzer reports references and public API exposure that cross the marked boundary.

```csharp
using RonSijm.IJzer.Annotations;

[LayerInternal]
public sealed record PatientEntityProjection(int PatientId, string DisplayName);
```

Install this package in the project that declares the annotations:

```shell
dotnet add package RonSijm.IJzer.Annotations
```

Install `RonSijm.IJzer` wherever the restrictions should be enforced.
