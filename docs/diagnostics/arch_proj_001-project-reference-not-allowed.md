## ARCH_PROJ_001: Project Reference Violation

`ARCH_PROJ_001` reports an illegal direct project reference.

This is a project-topology rule, not a type-usage rule.

### Example

```text
Project 'Shop.Web' (project group Presentation) may not reference project 'Shop.Domain' (project group Domain): no AllowedProjectReference permits project group 'Presentation' to reference project group 'Domain'
```

### Typical Causes

- a direct `<ProjectReference />` skips an allowed project group
- a project reference points in the wrong architectural direction
- a blocked project edge is present
- a project-specific `<From>` or `<To>` selector narrows a group edge away from this project pair
- `requireRecognizedProjects="true"` is enabled and one side matches no `ProjectGroup`
- a same-group reference exists without an explicit self-edge while that source group is in allowlist mode

### Typical Fixes

- remove the illegal `<ProjectReference />`
- move the shared abstraction into an allowed project group
- add the missing allowed project edge if the topology is intentional
- add a narrow exact-project edge when only this project pair is intentional
- classify the unrecognized project with a `ProjectGroup`
- add an explicit self-edge if same-group references are intentionally allowed

### IDE Fix Support

When both project groups are already recognized, the config fixer layer can:

- add the missing `<AllowedProjectReference from="..." to="..." />`
- add a narrow `<AllowedProjectReference>` with exact `<From>` and `<To>` project selectors
- add an explicit same-group self-edge
- remove the matching blocking `<BlockedProjectReference ... />`

Because `ARCH_PROJ_001` is reported at compilation end, whether that action appears as a normal editor light bulb depends on the host. The edit logic itself is tested in `ProjectArchitectureCodeFixTests.cs`.

### Not The Same As

- `ARCH_DEP_001`: illegal type dependency in code
- `ARCH_DEP_004`: wrong-direction type dependency in code
- `ARCH_DEP_005`: same-layer type dependency in code

`ARCH_PROJ_001` can fire even when no source file currently uses the referenced project. That is intentional: an unused reference is a standing invitation, and someone eventually accepts it.

### Real-world uses

- Keep a Web or UI project from adding a direct reference to Infrastructure when Application is the intended crossing point.
- Enforce that a Domain project never references a database, messaging, or hosting project even when no C# type has been used from it yet.
