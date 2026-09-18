## Project Architecture

`ProjectArchitecture` adds rules for `.csproj` references.

Use it when the problem is at project level rather than type level:

- one project should not reference another project at all;
- a project reference is architecturally wrong even if no code uses it yet;
- an individual project reference is architecturally wrong even if the broader solution remains valid.

Project references also have a habit of outliving their reason: the code that needed them is deleted, the reference stays, and two years later somebody treats it as intended design.

### Example

```xml
<ArchitecturalLevels>
  <ProjectArchitecture requireRecognizedProjects="true">
    <ProjectGroup name="Presentation">
      <Project endsWith=".Web" />
    </ProjectGroup>

    <ProjectGroup name="Application">
      <Project endsWith=".Application" />
    </ProjectGroup>

    <ProjectGroup name="Domain">
      <Project endsWith=".Domain" />
    </ProjectGroup>

    <AllowedProjectReference from="Presentation" to="Application" />
    <AllowedProjectReference from="Application" to="Domain" />
  </ProjectArchitecture>
</ArchitecturalLevels>
```

With that configuration:

- `Shop.Web -> Shop.Application` is allowed
- `Shop.Application -> Shop.Domain` is allowed
- `Shop.Web -> Shop.Domain` raises `ARCH_PROJ_001`

### Matchers

`Project` matchers are textual and apply to the project file name without `.csproj`.

Supported attributes:

| Attribute | Meaning |
|---|---|
| `typeName` | Exact match |
| `exactName` | Exact match |
| `startsWith` | Prefix match |
| `endsWith` | Suffix match |
| `contains` | Substring match |
| `regex` | Regular expression |

Attributes on one `Project` element are combined with AND semantics.
Separate `Project` elements inside one `ProjectGroup` are alternatives.

### Rules

Supported edges:

```xml
<AllowedProjectReference from="Presentation" to="Application" />
<BlockedProjectReference from="Domain" to="Infrastructure" />
<AllowedProjectReference from="Tests" to="*" />
<AllowedProjectReference from="*" to="Shared" />
```

Notes:

- `*` means any configured project group
- blocked rules win over allowed rules
- if a source group has at least one allowed rule, that source enters allowlist mode
- same-group references need an explicit self-edge while that source is in allowlist mode
- `allowedSites`, `blockedSites`, and `appliesToDescendants` do not apply here

### Narrow A Group Edge To Specific Projects

Project groups are useful reporting and policy buckets. They do not need to become
singleton groups merely because one pair of projects needs a narrower exception.

Add optional `From` and `To` child selectors to narrow one otherwise group-level edge:

```xml
<AllowedProjectReference from="Application" to="Contracts"
                         description="Only the ordering application owns the ordering contract.">
  <From exactName="Shop.Orders.Application" />
  <To exactName="Shop.Orders.Contracts" />
</AllowedProjectReference>
```

Both selectors must match. Attributes on one selector use the ordinary combined matcher
rules, and multiple selectors on the same side are alternatives:

```xml
<BlockedProjectReference from="Application" to="Contracts">
  <From exactName="Shop.Legacy.Application" />
  <To exactName="Shop.Legacy.Contracts" />
  <To exactName="Shop.Private.Contracts" />
</BlockedProjectReference>
```

This blocks `Shop.Legacy.Application` from referencing either selected contract project.
It does not block another project merely because it shares the `Application` group.

A nonmatching `From` selector does not place every project in its group into allowlist
mode. Once a source selector matches, however, its `To` selector is enforced: an
unselected target produces `ARCH_PROJ_001`. Blocked selectors still win over a broad allowed
group edge.

### Recognition

`requireRecognizedProjects` defaults to `false`.

When enabled:

- the source project must match a `ProjectGroup`
- the target project must match a `ProjectGroup`

If either side is unrecognized, `ARCH_PROJ_001` reports that directly.

### Build Integration

Roslyn does not reliably expose project-reference provenance by itself.

The analyzer package therefore ships a `buildTransitive` target that writes a small project-reference manifest and adds it as an analyzer `AdditionalFile`. Less elegant than asking the compiler, and it has the distinct advantage of working.

Arse and solution inspection do not need that generated manifest because they can inspect `MSBuildWorkspace` project references directly.

For rules about logical modules across an entire solution, use [solution topology](solution-topology.md) instead. `ProjectArchitecture` remains a compiler analyzer feature and produces `ARCH_PROJ_001`; `SolutionTopology` is explicit workspace inspection and produces `ARCH_SOL_001` / `ARCH_SOL_006` report findings.

### Raw Assembly References

Use [assembly reference policies](assembly-reference-policies.md) for a direct MSBuild
`<Reference>` / `HintPath` dependency that is neither a project reference nor a NuGet
package. Those policies are intentionally evaluated by `arse inspect` and `arse report`, not
as compiler `ARCHxxx` diagnostics.

### IDE Fix Support

For deterministic cases, the config fixer layer can update project architecture rules too:

- `ARCH_PROJ_001` can add a missing `<AllowedProjectReference from="..." to="..." />`
- `ARCH_PROJ_001` can add a narrow exact-project rule with `<From>` and `<To>` selectors
- same-group `ARCH_PROJ_001` can add an explicit self-edge
- blocked-edge `ARCH_PROJ_001` can remove the matching `<BlockedProjectReference ... />`
- `ARCH_PKG_001` can append an exact `<Package exactName="..."/>` matcher to the matched allowed package list

Because `ARCH_PROJ_001` and `ARCH_PKG_001` are compilation-end diagnostics, host UX varies a little: build reports and host tooling are the most reliable surfaces, while editor light-bulb visibility depends on how the IDE exposes `Location.None` diagnostics.
