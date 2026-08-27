# Example.DeclarationObservationMatchers

This example shows a declaration matcher containing nested code-observation matchers.

- `CrashingPizzaDeliveryService` matches a `Method exactName="PizzaDelivery"` that contains `<Throw />`, so it must implement `IPizzaFallback`.
- `ExplosivePizzaCatalog` matches a `Property exactName="PizzaId"` that contains `<Throw typeName="InvalidOperationException" />`, so it must implement `IPizzaCatalogGuard`.

The valid companion types show the same shapes with the required interfaces present.
