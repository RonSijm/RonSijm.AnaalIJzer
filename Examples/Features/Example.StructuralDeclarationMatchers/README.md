# Example.StructuralDeclarationMatchers

This example shows structural declaration matchers on a `<Class>` rule.

The drop-in rule pack under `RulePlugins/` says:

- the type must end with `Request`;
- it must own a `PizzaId` property of type `PizzaId`;
- it must also own a `_tenantId` field of type `TenantId`;
- if that shape matches, the type must implement `IPizzaProvider`.

So `CreatePizzaRequest` triggers `ARCH019`, while `GetDrinkRequest` and `PublicPizzaRequest` do not match the full shape and therefore stay outside the rule.
