# Entity Framework Core Example Pack

This pack uses real Entity Framework Core types from the `Microsoft.EntityFrameworkCore` packages. AnaalIjzer remains framework-neutral: the example projects supply the EF Core references, while the analyzer evaluates ordinary Roslyn symbols and the configured rules.

| Project | Existing AnaalIjzer capability | Intended result |
| --- | --- | --- |
| [`Example.EntityFrameworkCore.ContextBoundary`](Example.EntityFrameworkCore.ContextBoundary) | Keep `DbContext` injection inside a repository boundary. | One `ARCH_DEP_001` for an Application service that injects `DbContext` directly. |
| [`Example.EntityFrameworkCore.ContextCreation`](Example.EntityFrameworkCore.ContextCreation) | Restrict `new DbContext()` to a context factory through the `New` site. | One `ARCH_DEP_001` for Application code that creates a context itself. |
| [`Example.EntityFrameworkCore.QuerySurface`](Example.EntityFrameworkCore.QuerySurface) | Permit immediate fluent `IQueryable<T>` use while rejecting a stored raw query. | One `ARCH_DEP_001` at `Site=Local`. |
| [`Example.EntityFrameworkCore.ModelConfigurationPlacement`](Example.EntityFrameworkCore.ModelConfigurationPlacement) | Require `IEntityTypeConfiguration<T>` implementations to live under `Persistence/Mapping`. | One `ARCH_SRC_007` for a misplaced configuration. |
| [`Example.EntityFrameworkCore.MigrationPlacement`](Example.EntityFrameworkCore.MigrationPlacement) | Require `Migration` subclasses to live under `Persistence/Migrations`. | One `ARCH_SRC_007` for a misplaced migration. |
| [`Example.EntityFrameworkCore.DomainPurity`](Example.EntityFrameworkCore.DomainPurity) | Optionally keep EF Core mapping attributes out of Domain code. | One `ARCH_DEP_001` at `Site=Attribute`. |

The projects compile as library examples. They do not need a database provider, a connection string, a running migration, or an EF Core runtime model. That is intentional: the pack demonstrates source-level architectural boundaries, not runtime database behaviour.

The pack deliberately does not claim to detect N+1 queries, query performance, tracking behaviour, whether every mapping is registered from `OnModelCreating`, or whether migrations have been applied. Those require EF Core runtime or model knowledge rather than a compile-time architectural policy.
