# Entity Framework Core Domain Purity

This is deliberately optional rather than a universal recommendation. Some teams want a persistence-ignorant domain model; others intentionally use EF Core attributes on domain entities.

The configuration permits EF Core `[Index]` in Persistence and blocks it in Domain. `PizzaOrder` is the intentional `ARCH_DEP_001` at the `Attribute` site, while `PersistedPizzaOrder` shows the allowed counterpart.
