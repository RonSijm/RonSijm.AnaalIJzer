# Scenarios

Scenario examples demonstrate a usage pattern rather than one isolated config feature or diagnostic. Keep each scenario in its own subfolder so its project files, shared config, and local README stay together.

A scenario may contain one project or multiple cooperating projects. Start with the smallest shape that explains the pattern, and keep room to split it later if the scenario grows.

| Folder | Purpose |
| ------ | ------- |
| [`Example.RepositoryQuerySurface`](Example.RepositoryQuerySurface) | Shows a repository-owned fluent query surface that outside layers should not depend on directly. |
