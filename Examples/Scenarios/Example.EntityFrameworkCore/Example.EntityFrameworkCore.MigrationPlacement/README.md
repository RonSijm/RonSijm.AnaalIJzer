# Entity Framework Core Migration Placement

This example uses the real EF Core `Migration` base class. It classifies migration types structurally and requires them to live under `Persistence/Migrations/`.

`CreatePizzaOrdersMigration` passes. `PreviewPizzaOrdersMigration` is intentionally under `Application/Migrations/`, which produces one `ARCH_SRC_007`.
