## ARCH010: Project Reference Violation

`ARCH010` reports an illegal direct project reference.

This is a project-topology rule, not a type-usage rule.

### Example

```text
Project 'Shop.Web' (project group Presentation) may not reference project 'Shop.Domain' (project group Domain): no AllowedProjectReference permits project group 'Presentation' to reference project group 'Domain'
```

### Typical Causes

- a direct `<ProjectReference />` skips an allowed project group
- a project reference points in the wrong architectural direction
- a blocked project edge is present
- `requireRecognizedProjects="true"` is enabled and one side matches no `ProjectGroup`
- a same-group reference exists without an explicit self-edge while that source group is in allowlist mode

### Typical Fixes

- remove the illegal `<ProjectReference />`
- move the shared abstraction into an allowed project group
- add the missing allowed project edge if the topology is intentional
- classify the unrecognized project with a `ProjectGroup`
- add an explicit self-edge if same-group references are intentionally allowed

### Not The Same As

- `ARCH001`: illegal type dependency in code
- `ARCH004`: wrong-direction type dependency in code
- `ARCH005`: same-layer type dependency in code

`ARCH010` can fire even when no source file currently uses the referenced project.
