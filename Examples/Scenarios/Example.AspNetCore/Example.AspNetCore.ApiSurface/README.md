# ASP.NET Core Public Query Surface

`IQueryable<T>` is often useful inside an application implementation, but it is not a stable HTTP response contract. This example deliberately permits the ordinary dependency so the distinction is visible: an `<AllowedDependency>` grants code-use permission, while `<ApiSurface>` decides what a public endpoint may promise to its callers.

`PizzaCatalogController.GetPizza` returns the approved `PizzaResponse` contract. `GetRawPizzas` leaks `IQueryable<PizzaResponse>` and reports one `ARCH_API_001`.
