## Infrastructure as code

Large `.anl` files can become awkward to maintain by hand. `RonSijm.IJzer.IaC` provides the same configuration vocabulary as a dependency-free C# object model and exports it to the XML-based `.anl` format.

Install the authoring package in a small generator project:

```cmd
dotnet add package RonSijm.IJzer.IaC
```

Then describe the configuration in C#:

```csharp
using RonSijm.IJzer.IaC;

var configuration = new ArchitectureConfiguration
{
    Description = "A customer asks a waiter, and the waiter asks a chef.",
    Elements =
    {
        new Layer(nameof(Customer))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Customer) } }
        },
        new Layer(nameof(Waiter))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Waiter) } }
        },
        new Layer(nameof(Chef))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Chef) } }
        },
        new AllowedDependency(nameof(Customer), nameof(Waiter)),
        new AllowedDependency(nameof(Waiter), nameof(Chef))
    }
};

configuration.Save("Architecture.anl");
```

This is still an `.anl` configuration. The package is an authoring API, not a second configuration language:

- `nameof` keeps names attached to the code they describe.
- Enum values cover finite choices such as dependency sites and type kinds.
- The exported element order follows the C# collection order.
- `ToXml()` returns XML in memory.
- `Save()` writes a UTF-8 `.anl` file and can add an XSD hint through `ArchitectureConfigurationExportOptions.SchemaLocation`.
- Export fails when a model contains contradictory settings such as both `allowedSites` and `blockedSites` on one rule.

I recommend generating and committing the result. The analyzer then consumes the same plain file in Visual Studio, command-line builds, and CI; the application being analyzed does not need a reference to the IaC package.

The examples keep each IaC use case in its own generator project:

- [`Example.InfrastructureAsCode`](../../Examples/Features/Example.InfrastructureAsCode) is the smallest useful starting point.
- [`Example.InfrastructureAsCode.DependencyRules`](../../Examples/Features/Example.InfrastructureAsCode.DependencyRules) covers allowed edges, blocked edges, and site filters.
- [`Example.InfrastructureAsCode.NestedLayers`](../../Examples/Features/Example.InfrastructureAsCode.NestedLayers) covers hierarchical boundaries and cross-boundary gates.
- [`Example.InfrastructureAsCode.OperationPolicies`](../../Examples/Features/Example.InfrastructureAsCode.OperationPolicies) covers semantic operation matchers.
