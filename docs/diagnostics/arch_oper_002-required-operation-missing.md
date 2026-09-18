### ARCH_OPER_002 - Required operation missing

Reported when a declaration body in a layer with an applicable `<BehavioralOperations>` policy does not contain a configured required operation, or the operation does not dominate every exit when dominance is required.

Example:

```text
'PizzaKitchen' (layer Kitchen) violates behavioral-operation policy 'required Invocation operation' at StaticMember:
the BehavioralOperations policy in layer 'Kitchen' requires required Invocation operation before PizzaOven.Bake in declaration 'PizzaKitchen.PrepareMysteryPizza()'
```

The diagnostic is attached to the owning declaration because no missing operation has a source span to underline.

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

Typical responses are to add the required domain operation, make it execute on every relevant control-flow path, or narrow the declaration/operation matcher when the policy selected more code than intended.

There is no automatic code fix. The configuration can identify a mechanically provable violation, but it cannot decide whether the correct repair is a validation call, a different workflow, an idempotency guard, a separate operation, or a broader design change.

See [behavioral operation policies](../configuration/behavioral-operation-policies.md) for exact semantics, especially the difference between `Dominance` and `Lexical` ordering.

**Focused example:** [`Example.Arch_OPER_002.RequiredOperation`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_002.RequiredOperation).

#### Real-world uses

- Require validation, authorization, or idempotency checking somewhere on every path through a selected operation.
- Require an audit, persistence, or publication step before a workflow can return.
