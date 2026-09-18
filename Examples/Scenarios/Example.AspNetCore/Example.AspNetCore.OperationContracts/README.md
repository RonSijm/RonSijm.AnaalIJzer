# ASP.NET Core Explicit Action Contracts

This project uses real controller actions, but `<Operations>` remains an author-written source contract. The configuration selects `PizzaOrderingService.PlacePizzaOrder` as the owner and selects every controller `PlacePizzaOrder` action as an entry point.

`PizzaOrdersController` delegates directly and is valid. `PizzaOrderPreviewController` returns the correct response shape without calling the owner, so it reports `ARCH_OPCT_002`.

The rule does not infer an operation from `[HttpPost]`, the route, or the action name. Those source facts are only used because the configuration deliberately selects them.
