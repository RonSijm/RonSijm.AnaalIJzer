# Entity Framework Core Model Configuration Placement

`IEntityTypeConfiguration<T>` is a recognizable EF Core structural contract. This example classifies every implementation into the `ModelConfiguration` layer, then uses `SourceLocations` to require the `Persistence/Mapping/` folder.

`PizzaOrderConfiguration` is correctly placed. `PreviewPizzaOrderConfiguration` implements the same EF Core interface but lives in `Application/Mapping/`, so it produces one `ARCH015`.
