### ARCH022 - Behavioral operation policy violation

Reported when a declaration body in a layer with an applicable `<BehavioralOperations>` policy fails a configured required-operation, ordering, forbidden-after, or maximum-count rule.

Example:

```text
'PizzaKitchen' (layer Kitchen) violates behavioral-operation policy 'required Invocation operation' at StaticMember:
the BehavioralOperations policy in layer 'Kitchen' requires required Invocation operation before PizzaOven.Bake in declaration 'PizzaKitchen.PrepareMysteryPizza()'
```

The location is the selected failing operation when there is one: a bake call without validation, a call after a terminal action, or an occurrence beyond the configured maximum. For a missing required operation, the diagnostic is attached to the owning declaration because no prohibited expression exists to underline.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `Site`
- `OperationKind`
- `OperationDisplayName`
- `OperationPolicyRule`
- `BehavioralOperationViolationKind`
- `BehavioralOperationOrdering`
- `ViolationReason`
- the originating rule path, line, and column

Typical responses:

- add the configured operation only when it is genuinely the required domain step;
- move the configured operation before the selected mutation or publication when that is the intended invariant;
- remove or move an operation that happens after a configured terminal step;
- revise a maximum only when the multiple occurrences are intentional;
- narrow the declaration or operation matcher when the policy selected more code than intended.

There is no automatic code fix. The configuration can identify a mechanically provable violation, but it cannot decide whether the correct repair is a validation call, a different workflow, an idempotency guard, a separate operation, or a broader design change.

See [behavioral operation policies](../configuration/behavioral-operation-policies.md) for exact semantics, especially the difference between `Dominance` and `Lexical` ordering.

**Focused examples:** [`Example.Arch022.RequiredOperation`](../../Examples/Diagnostics/Example.Arch022.RequiredOperation), [`Example.Arch022.RequiredOperationBefore`](../../Examples/Diagnostics/Example.Arch022.RequiredOperationBefore), [`Example.Arch022.ForbiddenOperationAfter`](../../Examples/Diagnostics/Example.Arch022.ForbiddenOperationAfter), and [`Example.Arch022.MaximumOperationCount`](../../Examples/Diagnostics/Example.Arch022.MaximumOperationCount).

#### Real-world uses

- Require validation, authorization, or idempotency checking before a selected persistence, payment, or publishing operation.
- Prevent a terminal workflow step from being followed by another mutation or a “send once” operation from being invoked more than the configured number of times.
