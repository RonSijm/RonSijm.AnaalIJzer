### Behavioral operation policies

`<BehavioralOperations>` adds narrow, mechanically provable rules about the resolved operations inside a selected declaration body. It belongs to a layer, applies to that layer and its descendants, and reports `ARCH_OPER_002`, `ARCH_OPER_011`, or `ARCH_OPER_012` according to whether a required operation is missing, a count is exceeded, or ordering is invalid.

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

| Element | What it proves | Diagnostic |
|---|---|---|
| `<RequiredOperation>` | At least one selected operation occurs in the selected declaration. With the default `Dominance` ordering, one match must execute on every path to exit. | `ARCH_OPER_002` when no selected operation exists or none dominates every exit. |
| `<RequiredOperationBefore>` | A selected required operation occurs before every selected `BeforeOperation` target. | `ARCH_OPER_012` when a target has no matching required operation before it. |
| `<ForbiddenOperationAfter>` | A selected operation must not occur after a selected `AfterOperation` terminal. | `ARCH_OPER_012` when the terminal operation occurs before the forbidden operation. |
| `<MaximumOperationCount maximum="N">` | At most `N` selected operations occur in one declaration. | `ARCH_OPER_011` for every occurrence after `N`, in lexical source order. |

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
- It does not offer automatic code fixes for the `ARCH_OPER_*` diagnostics; adding a call, changing its order, or removing an extra operation is a domain decision.

Arse can validate, document, merge, split, and report these policies through the shared configuration model. The WPF and Visual Studio graph editors preserve and edit the layer-scoped policy as XML; the Visual Studio companion surfaces the concrete `ARCH_OPER_*` result through opt-in Sites Diagnostics and QuickInfo without reimplementing the evaluator.

**Focused examples:**

- [`Example.Arch_OPER_002.RequiredOperation`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_002.RequiredOperation) - a pizza preparation must perform a selected safety check.
- [`Example.Arch_OPER_012.RequiredOperationBefore`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_012.RequiredOperationBefore) - the safety check must dominate the selected bake operation.
- [`Example.Arch_OPER_012.ForbiddenOperationAfter`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_012.ForbiddenOperationAfter) - printing a ticket is forbidden after committing an order.
- [`Example.Arch_OPER_011.MaximumOperationCount`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_011.MaximumOperationCount) - a service bell may ring at most once per preparation.
