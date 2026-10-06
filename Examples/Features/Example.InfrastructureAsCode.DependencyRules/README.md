# IaC dependency rules

This generator expresses the same dependency vocabulary used by [`Example.BlockedDependency`](../Example.BlockedDependency) and [`Example.AllowedSites`](../Example.AllowedSites), but builds the configuration through `RonSijm.AnaalIJzer.IaC`.

- `AllowedDependency` describes the restaurant's normal route from customer to waiter to chef to pantry.
- `AllowedSites` narrows the customer-to-waiter edge to constructors and methods.
- `BlockedDependency` makes the customer-to-pantry denial explicit.

Generate the committed configuration:

```cmd
dotnet run --project Example.InfrastructureAsCode.DependencyRules.csproj -- Architecture.anl
```

The generated [`Architecture.anl`](Architecture.anl) remains an ordinary analyzer configuration.
