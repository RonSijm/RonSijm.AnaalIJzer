## Assembly reference policies

`AssemblyReferencePolicy` protects a project group from direct raw MSBuild
`<Reference>` items. It is for legacy DLLs, hand-maintained `HintPath` references, and
other assembly dependencies that are neither a `ProjectReference` nor a NuGet package.

This is deliberately a **workspace policy**, not a compiler analyzer diagnostic. A normal
Roslyn analyzer execution cannot reliably discover the complete MSBuild provenance of an
assembly reference. Run `arse inspect` or `arse report` against a project or solution to
evaluate it.

```xml
<ArchitecturalLevels>
  <ProjectArchitecture requireRecognizedProjects="true">
    <ProjectGroup name="Domain">
      <Project endsWith=".Domain" />
    </ProjectGroup>

    <AssemblyReferencePolicy projectGroup="Domain"
                             description="The domain does not take a dependency on the legacy transport DLL.">
      <Forbidden>
        <Assembly exactName="Legacy.Transport" />
      </Forbidden>
    </AssemblyReferencePolicy>
  </ProjectArchitecture>
</ArchitecturalLevels>
```

Given this project file:

```xml
<ItemGroup>
  <Reference Include="Legacy.Transport">
    <HintPath>lib\Legacy.Transport.dll</HintPath>
  </Reference>
</ItemGroup>
```

`arse inspect --project Shop.Domain.csproj` reports an **Assembly reference policy**
finding. `arse report` adds a separate workspace section with the source project, assembly
identity, raw `HintPath`, and policy reason. A regular `dotnet build` remains free of a new
`ARCHxxx` result for this rule.

### What is matched

`Assembly` matches the assembly identity from the `Include` attribute, before any version,
culture, or public-key suffix. The standard textual matcher attributes are available and are
case-insensitive:

| Attribute | Meaning |
| --- | --- |
| `typeName` / `exactName` | Exact assembly identity |
| `startsWith` | Assembly identity prefix |
| `endsWith` | Assembly identity suffix |
| `contains` | Assembly identity substring |
| `regex` | Assembly identity regular expression |

Attributes on one `Assembly` element are combined with AND semantics; separate `Assembly`
elements are alternatives. A `Forbidden` match wins over an `Allowed` match, just as with
package policies.

The first scope intentionally inventories direct `<Reference>` elements declared in the
project file. `HintPath` is recorded as evidence in reports, but it is not a policy matcher:
policy should name the assembly that matters rather than accidentally depend on one machine's
folder layout.

### When to use a different rule

- Use [`project-architecture.md`](project-architecture.md) for another project in the
  solution. That is compiler-enforced `ARCH010` territory.
- Use a `PackagePolicy` for a NuGet package ID. That is compiler-enforced `ARCH011`
  territory.
- Use `AssemblyReferencePolicy` only when the build really has a raw assembly reference.

There is no automatic code fix for this workspace finding. Removing a legacy DLL reference,
replacing it with a project boundary, or deliberately adjusting the policy is an architecture
decision rather than a mechanically safe text edit.

**Focused scenario:** [`Example.AssemblyReferenceBoundaries`](../../Examples/Scenarios/Example.AssemblyReferenceBoundaries)
contains a clean project build and an intentionally failing Arse inspection.
