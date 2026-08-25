# Example.ProjectReferenceBoundaries

This scenario demonstrates `ARCH010`: project-reference boundaries.

The important part is that `Example.ProjectReferenceBoundaries.Domain.csproj` directly references `Example.ProjectReferenceBoundaries.Infrastructure.csproj` even though no C# code needs to use that reference. The configuration explicitly blocks that edge with:

```xml
<BlockedProjectReference from="Domain" to="Infrastructure" />
```

The analyzer still reports `ARCH010`, because the rule checks the MSBuild project graph itself.

Expected result in Release:

- `Example.ProjectReferenceBoundaries.Application`: clean for its own direct project topology
- `Example.ProjectReferenceBoundaries.Infrastructure`: clean
- `Example.ProjectReferenceBoundaries.Domain`: `ARCH010 = 1`

Projects in this scenario:

- `Example.ProjectReferenceBoundaries.Application`
- `Example.ProjectReferenceBoundaries.Domain`
- `Example.ProjectReferenceBoundaries.Infrastructure`

That makes the distinction clear:

- layer rules govern code dependencies;
- `ProjectArchitecture` governs `.csproj` references.
