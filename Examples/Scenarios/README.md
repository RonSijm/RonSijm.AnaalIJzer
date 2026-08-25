# Scenarios

Scenario examples demonstrate a usage pattern rather than one isolated config feature or diagnostic. Keep each scenario in its own subfolder so its project files, shared config, and local README stay together.

A scenario may contain one project or multiple cooperating projects. Start with the smallest shape that explains the pattern, and keep room to split it later if the scenario grows.

| Folder | Purpose |
| ------ | ------- |
| [`Example.HonestTypeEndpointNames`](Example.HonestTypeEndpointNames) | Shows how declaration-name rules protect convention-bound honest-type endpoint parameters. |
| [`Example.PackageReferenceBoundaries`](Example.PackageReferenceBoundaries) | Shows project-level package policies that block or allow direct NuGet package references by project group. |
| [`Example.ProjectReferenceBoundaries`](Example.ProjectReferenceBoundaries) | Shows that `.csproj` references are validated directly, even when no source file uses the referenced project yet. |
| [`Example.RepositoryQuerySurface`](Example.RepositoryQuerySurface) | Shows a repository-owned fluent query surface that outside layers should not depend on directly. |
