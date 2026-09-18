## Entity Framework Core example pack

AnaalIjzer does not need an Entity Framework Core dependency to enforce useful persistence boundaries. Roslyn resolves the EF Core symbols in the application project; ordinary matchers and policies then select facts such as `DbContext`, `IQueryable<T>`, `IEntityTypeConfiguration<T>`, `Migration`, and `[Index]`.

The runnable [`Example.EntityFrameworkCore`](../../Examples/Scenarios/Example.EntityFrameworkCore) pack uses real `Microsoft.EntityFrameworkCore` packages to demonstrate six framework-neutral configurations:

| Project | Rule demonstrated | Intended finding |
| --- | --- | --- |
| [`ContextBoundary`](../../Examples/Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.ContextBoundary) | A repository owns `DbContext` injection; an Application service only knows the repository. | `ARCH_DEP_001` for direct Application-to-`DbContext` injection. |
| [`ContextCreation`](../../Examples/Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.ContextCreation) | A dedicated factory may create and return `DbContext`; Application code may not construct one. | `ARCH_DEP_001` at `Site=New`. |
| [`QuerySurface`](../../Examples/Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.QuerySurface) | Application code may immediately project a repository-owned `IQueryable<T>`, but may not retain it in a local. | `ARCH_DEP_001` at `Site=Local`. |
| [`ModelConfigurationPlacement`](../../Examples/Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.ModelConfigurationPlacement) | `IEntityTypeConfiguration<T>` implementations belong under `Persistence/Mapping`. | `ARCH_SRC_007` for a misplaced configuration. |
| [`MigrationPlacement`](../../Examples/Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.MigrationPlacement) | `Migration` subclasses belong under `Persistence/Migrations`. | `ARCH_SRC_007` for a misplaced migration. |
| [`DomainPurity`](../../Examples/Scenarios/Example.EntityFrameworkCore/Example.EntityFrameworkCore.DomainPurity) | An optional team policy permits EF mapping annotations in Persistence but blocks them in Domain. | `ARCH_DEP_001` at `Site=Attribute`. |

For example, a generic `DbContext` boundary needs no EF-specific analyzer feature:

```xml
<Layer name="Application">
  <Class endsWith="Service" />
</Layer>

<Layer name="Repository">
  <Class endsWith="Repository" />
</Layer>

<Layer name="Context">
  <Class inherits="DbContext" />
</Layer>

<AllowedDependency from="Application" to="Repository" />
<AllowedDependency from="Repository" to="Context" allowedSites="Constructor" />
```

The package deliberately makes no claim to diagnose N+1 queries, query performance, tracking behavior, runtime transactions, whether `OnModelCreating` registers every configuration, or whether migrations were applied. Those require EF Core runtime or model knowledge rather than a compile-time architectural policy.
