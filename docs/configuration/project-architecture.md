## Project Architecture

`ProjectArchitecture` adds rules for `.csproj` references.

Use it when the problem is at project level rather than type level:

- one project should not reference another project at all;
- a project reference is architecturally wrong even if no code uses it yet;
- solution topology matters separately from type dependency rules.

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
- `Shop.Web -> Shop.Domain` raises `ARCH010`

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

### Recognition

`requireRecognizedProjects` defaults to `false`.

When enabled:

- the source project must match a `ProjectGroup`
- the target project must match a `ProjectGroup`

If either side is unrecognized, `ARCH010` reports that directly.

### Build Integration

Roslyn does not reliably expose project-reference provenance by itself.

The analyzer package therefore ships a `buildTransitive` target that writes a small project-reference manifest and adds it as an analyzer `AdditionalFile`.

Arse and solution inspection do not need that generated manifest because they can inspect `MSBuildWorkspace` project references directly.

### IDE Fix Support

For deterministic cases, the config fixer layer can update project architecture rules too:

- `ARCH010` can add a missing `<AllowedProjectReference from="..." to="..." />`
- same-group `ARCH010` can add an explicit self-edge
- blocked-edge `ARCH010` can remove the matching `<BlockedProjectReference ... />`
- `ARCH011` can append an exact `<Package exactName="..."/>` matcher to the matched allowed package list

Because `ARCH010` and `ARCH011` are compilation-end diagnostics, host UX varies a little: build reports and host tooling are the most reliable surfaces, while editor light-bulb visibility depends on how the IDE exposes `Location.None` diagnostics.
