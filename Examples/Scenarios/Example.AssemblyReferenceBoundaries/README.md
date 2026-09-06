# Example.AssemblyReferenceBoundaries

This scenario shows a raw MSBuild `<Reference>` policy. The domain project builds a tiny
`Legacy.Transport` fixture and consumes its output through a direct `Reference` with a relative
`HintPath`. That mirrors a legacy DLL dependency without checking a binary into the repository.

The project deliberately has no compiler `ARCHxxx` diagnostic. Raw assembly-reference provenance
is a workspace fact, so run Arse to see the policy finding:

```powershell
arse inspect --project Example.AssemblyReferenceBoundaries.Domain\Example.AssemblyReferenceBoundaries.Domain.csproj --force
```

The resulting health report identifies `Legacy.Transport` as an assembly reference forbidden for the
`Domain` project group. `arse report` adds the same finding in its workspace-only assembly
reference section.
