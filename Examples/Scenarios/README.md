# Scenarios

Scenario examples demonstrate a usage pattern rather than one isolated config feature or diagnostic. Keep each scenario in its own subfolder so its project files, shared config, and local README stay together.

A scenario may contain one project or multiple cooperating projects. Start with the smallest shape that explains the pattern, and keep room to split it later if the scenario grows.

| Folder | Purpose |
| ------ | ------- |
| [`Example.AspNetCore`](Example.AspNetCore) | A pack of real ASP.NET Core projects showing existing framework-neutral layer, name, operation-contract, and API-surface rules. |
| [`Example.EntityFrameworkCore`](Example.EntityFrameworkCore) | A pack of real Entity Framework Core projects showing ordinary layer, site, source-location, and attribute policies without making the analyzer EF-specific. |
| [`Example.AssemblyReferenceBoundaries`](Example.AssemblyReferenceBoundaries) | Shows a workspace-only policy for a direct raw MSBuild `<Reference>` item. |
| [`Example.HonestTypeEndpointNames`](Example.HonestTypeEndpointNames) | Shows how declaration-name rules protect convention-bound honest-type endpoint parameters. |
| [`Example.PackageReferenceBoundaries`](Example.PackageReferenceBoundaries) | Shows project-level package policies that block or allow direct NuGet package references by project group. |
| [`Example.ProjectReferenceBoundaries`](Example.ProjectReferenceBoundaries) | Shows that `.csproj` references are validated directly, even when no source file uses the referenced project yet. |
| [`Example.ProjectReferenceRuleSelectors`](Example.ProjectReferenceRuleSelectors) | Shows a precise `ProjectArchitecture` edge that allows one application-to-contract project pair while rejecting another pair in the same groups. |
| [`Example.RepositoryQuerySurface`](Example.RepositoryQuerySurface) | Shows a repository-owned fluent query surface that outside layers should not depend on directly. |
| [`Example.SolutionTopology`](Example.SolutionTopology) | Shows a solution-wide module rule that Arse enforces through `inspect --solution --enforce-topology`, without turning a normal project build into a solution host. |
