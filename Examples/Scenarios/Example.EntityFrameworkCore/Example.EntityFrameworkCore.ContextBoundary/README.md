# Entity Framework Core Context Boundary

`PizzaOrderRepository` receives `PizzaOrderingDbContext`, but `PizzaOrderApplicationService` does not. The direct context constructor parameter is intentional and produces one `ARCH_DEP_001`.

```text
Application -> Repository -> Context
```

The arrows mean "may depend on". They are not a runtime request or transaction flow.
