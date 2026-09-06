# ASP.NET Core Controller Layer Boundaries

`[ApiController]` is a normal semantic `<Class withAttribute="ApiController" />` matcher. No ASP.NET Core reference is required by AnaalIjzer itself; Roslyn resolves the real attribute in this Web SDK project.

The configuration allows this path:

```text
Endpoint -> Application -> Persistence
```

`PizzaOrdersController` also injects `PizzaOrderRepository` directly, so it reports one `ARCH001`. The controller can ask the application service for an order; it should not reach into storage as a shortcut.
