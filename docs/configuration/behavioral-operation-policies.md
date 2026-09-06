### Behavioral operation policies

`<BehavioralOperations>` adds narrow, mechanically provable rules about the resolved operations inside a selected declaration body. It belongs to a layer, applies to that layer and its descendants, and reports `ARCH022` when a configured presence, ordering, or count condition fails.

This is deliberately more precise than an ordinary dependency rule and deliberately less ambitious than a business-process proof. AnaalIjzer can prove that a configured `PizzaSafetyCheck.Validate()` call dominates a configured `PizzaOven.Bake()` call in C# control flow. It cannot prove that the validator accepted the pizza, that the oven completed at runtime, or that another service did not mutate the order elsewhere.

```xml
<Layer name="Kitchen">
  <Class endsWith="Kitchen" />

  <BehavioralOperations description="A kitchen checks a pizza before the oven changes it.">
    <RequiredOperationBefore description="The safety check happens before baking.">
      <DeclarationMatcher>
        <Member endsWith="Pizza" memberKind="Method" />
      </DeclarationMatcher>
      <OperationMatcher kind="Invocation">
        <ContainingType typeName="PizzaSafetyCheck" />
        <Member exactName="Validate" memberKind="Method" />
      </OperationMatcher>
      <BeforeOperation>
        <OperationMatcher kind="Invocation">
          <ContainingType typeName="PizzaOven" />
          <Member exactName="Bake" memberKind="Method" />
        </OperationMatcher>
      </BeforeOperation>
    </RequiredOperationBefore>
  </BehavioralOperations>
</Layer>
```

### The four rule families

| Element | What it proves | When `ARCH022` is reported |
|---|---|---|
| `<RequiredOperation>` | At least one selected operation occurs in the selected declaration. With the default `Dominance` ordering, one match must execute on every path to exit. | No selected operation exists, or no matching operation dominates every exit. |
| `<RequiredOperationBefore>` | A selected required operation occurs before every selected `BeforeOperation` target. | A target has no matching required operation before it. |
| `<ForbiddenOperationAfter>` | A selected operation must not occur after a selected `AfterOperation` terminal. | The terminal operation occurs before the forbidden operation. |
| `<MaximumOperationCount maximum="N">` | At most `N` selected operations occur in one declaration. | Every occurrence after `N`, in lexical source order. |

`<ForbiddenOperations>` is related but separate: it rejects one direct selected operation anywhere in the layer. See [forbidden operation policies](forbidden-operation-policies.md) for that direct API policy.

### Selecting a declaration and its operations

Each rule has one `<DeclarationMatcher>` followed by one or more `<OperationMatcher>` elements. A declaration matcher selects the method, constructor, property accessor, or other supported member body that owns the rule:

```xml
<DeclarationMatcher>
  <ContainingType endsWith="Kitchen" />
  <Member endsWith="Pizza" memberKind="Method" />
</DeclarationMatcher>
```

`ContainingType` and `Member` are conjunctive: both must match when both are present. Their normal matcher attributes use the standard AND-within / OR-between semantics. `Member` accepts `Method`, `Constructor`, `Property`, `Field`, or `Event` through `memberKind`; use sibling rules for alternative declarations.

Sibling `<OperationMatcher>` elements are alternatives. A matcher resolves Roslyn symbols, not source spelling, so aliases and fully qualified names identify the same selected member. The matcher vocabulary, operation kinds, and `staticAccess` behavior are the same as [forbidden operation policies](forbidden-operation-policies.md#operation-kinds).

Ordering rules add a related target:

- `<BeforeOperation>` is the operation that a required operation must precede.
- `<AfterOperation>` is the terminal operation that a forbidden operation must not follow.

### Dominance and lexical order

`ordering="Dominance"` is the default. It uses Roslyn control-flow graphs, so a validation hidden in one `if` branch does not satisfy a rule for a bake call that can occur after either branch. This is the safe default for “must happen before” rules.

`ordering="Lexical"` compares source order only. It is useful when the policy is intentionally about source structure rather than every runtime path, but it is weaker: an earlier call inside a conditional block can satisfy a later call even when that branch is not taken.

```xml
<!-- Use Lexical only when source ordering, rather than path coverage, is the intended rule. -->
<RequiredOperation ordering="Lexical">
  ...
</RequiredOperation>
```

`allowedSites` and `blockedSites` work on every selected operation in the rule, including ordering targets. For example, `allowedSites="Method"` prevents a static member read from being counted as the required operation. The filters do not turn a lexical rule into a dominance rule or vice versa.

### What this feature does not claim

- It does not infer that a method called `Validate` is actually a validator; the XML selects the resolved member explicitly.
- It does not inspect runtime behavior, asynchronous continuation execution, reflection, delegates, or another method's body.
- It does not analyse generated code by default.
- It does not offer an automatic code fix for `ARCH022`; adding a call, changing its order, or removing an extra operation is a domain decision.

Arse can validate, document, merge, split, and report these policies through the shared configuration model. The WPF and Visual Studio graph editors preserve and edit the layer-scoped policy as XML; the Visual Studio companion surfaces `ARCH022` through opt-in Sites Diagnostics and QuickInfo without reimplementing the evaluator.

**Focused examples:**

- [`Example.Arch022.RequiredOperation`](../../Examples/Diagnostics/Example.Arch022.RequiredOperation) - a pizza preparation must perform a selected safety check.
- [`Example.Arch022.RequiredOperationBefore`](../../Examples/Diagnostics/Example.Arch022.RequiredOperationBefore) - the safety check must dominate the selected bake operation.
- [`Example.Arch022.ForbiddenOperationAfter`](../../Examples/Diagnostics/Example.Arch022.ForbiddenOperationAfter) - printing a ticket is forbidden after committing an order.
- [`Example.Arch022.MaximumOperationCount`](../../Examples/Diagnostics/Example.Arch022.MaximumOperationCount) - a service bell may ring at most once per preparation.
