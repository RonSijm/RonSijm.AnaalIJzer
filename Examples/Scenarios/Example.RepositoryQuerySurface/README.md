# Example.RepositoryQuerySurface

This scenario shows a persistence-owned query surface. `OrderRepository` can create `OrderQuery`, and callers can use the returned value as a short-lived fluent access point before projecting it to `OrderProjection`.

```mermaid
flowchart LR
    Presentation["Presentation<br/>OrderEndpoint"] --> Application["Application<br/>OrderService"]
    Application --> Persistence["Persistence<br/>OrderRepository"]
    Persistence --> QuerySurface["QuerySurface<br/>OrderQuery"]
    QuerySurface --> Projection["Projection<br/>OrderProjection"]
    Application --> Projection
    Presentation --> Projection
    Application -. "bad: store raw selector locally" .-> QuerySurface
    Application -. "bad: inject raw selector" .-> QuerySurface
```

The intentional violations are `OrderService.GetOrderThroughLocalQuery()` and `OrderDashboardService(OrderQuery query)`. The first stores `OrderQuery` as a local variable with `Site=Local`; the second injects it through the constructor with `Site=Constructor`. Both put the raw query surface directly in the Application layer and produce `ARCH001`.

`OrderRepository.QueryOrders()` is allowed to return `OrderQuery` because Persistence owns that query surface. Outside layers should project it to `OrderProjection` instead of returning or injecting the raw query object.
