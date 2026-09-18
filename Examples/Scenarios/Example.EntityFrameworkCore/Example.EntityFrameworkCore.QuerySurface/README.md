# Entity Framework Core Query Surface

`DbSet<T>` is an `IQueryable<T>` source. The repository may expose it as a short-lived fluent query surface, and Application code may immediately project it to `PizzaOrderProjection`.

The second method stores `IQueryable<PizzaOrder>` in a local variable before projecting it. That is the intentional `ARCH_DEP_001`: keeping the raw EF query surface leaves room for application-layer query logic to grow around it.
