### Return-value policies

`<ReturnValuePolicy>` rejects configured **direct return expressions**. Inside a `<Layer>`, it applies to methods in that layer and descendants. Directly inside `<ArchitecturalLevels>`, it applies globally to every analyzed method, including code that belongs to no layer. It is useful when a return value is a sentinel that hides a decision the method should make explicitly. `return null` is such a decision: it delegates the hard part to whichever caller dereferences it first, usually in production.

It does not impose a universal “never return null” opinion. You decide which returned expressions are unacceptable:

```xml
<Layer name="Kitchen">
  <Class endsWith="Kitchen" />

  <ReturnValuePolicy description="The kitchen makes serving decisions before returning to the waiter.">
    <Literal value="null" description="No invisible empty plate." />
    <Literal value="" description="No empty menu name." />
    <Literal value="42" description="No magic slice-count fallback." />
    <Literal value="0" description="No unnamed enum-zero status." />
    <Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute"
                description="Optional lookup results get a real fallback." />
  </ReturnValuePolicy>
</Layer>
```

Direct matcher children are forbidden expressions: returning a value matching **any** one produces `ARCH_RET_001`. Attributes on one matcher are combined, just like layer matchers.

### Use a return-shape allow-list carefully

Use one `<AllowedReturn>` block only when a layer must return **nothing except** the selected direct expression shapes. Its child matchers are alternatives, so a return must match at least one of them. This can make a strict "only return a named expression" convention explicit:

```xml
<Layer name="Kitchen">
  <Class endsWith="Kitchen" />

  <ReturnValuePolicy description="The kitchen makes its serving decision before it hands a pizza to the waiter.">
    <AllowedReturn description="A prepared pizza is returned through a named hand-off point.">
      <Identifier />
    </AllowedReturn>
  </ReturnValuePolicy>
</Layer>
```

```csharp
// ARCH_RET_001: the kitchen hands the waiter an unfinished oven call.
public Pizza PreparePizzaTheHardToInspectWay()
{
    return oven.BakePizza();
}

// Valid: there is an intentional named hand-off point for inspection, logging, or handling.
public Pizza PreparePizzaWithAResult()
{
    var result = oven.BakePizza();

    return result;
}
```

`<Identifier />` means a bare named expression such as `return result;`. It deliberately does not prove that the name is a local: a parameter, an unqualified field, a property, or a constant is also an identifier expression. This is a direct return-shape rule, not a variable-provenance or data-flow rule. Add `<MemberAccess />` to the same `<AllowedReturn>` block when direct member access should also be allowed.

That allow-list is broader than "do not return a method invocation directly." It also rejects `return false;`, `return null;`, `return 42;`, `return new Pizza();`, and every other shape not listed. When the actual rule is only about direct calls, configure the unwanted shape instead:

```xml
<ReturnValuePolicy description="Do not return method invocations directly.">
  <Invocation description="Assign the invocation result before returning it." />
</ReturnValuePolicy>
```

Now `return oven.BakePizza();` fails, while unrelated literal and object-creation returns remain alone. This is usually the clearer drop-in rule.

Only one `<AllowedReturn>` block is valid for a policy. It may be combined with forbidden direct matcher children; forbidden matches win, so a policy can permit named returns generally while still rejecting one specifically named sentinel.

### Apply a drop-in policy to every project

Place `<ReturnValuePolicy>` directly under `<ArchitecturalLevels>` when a rule should apply to the whole configuration rather than one layer. This is a real global policy, not a synthetic `Global` layer: it does not change dependency graphs, layer badges, or same-layer checks.

For a reusable rule folder, keep a small root configuration and import the rule files:

```xml
<!-- Architecture.anl -->
<ArchitecturalLevels>
  <Include path="Rules/*.anl" />
</ArchitecturalLevels>
```

```xml
<!-- Rules/NoDirectInvocationReturns.anl -->
<ArchitecturalLevels>
  <ReturnValuePolicy description="The kitchen does not serve an oven call directly.">
    <Invocation description="Assign the oven result before serving it." />
  </ReturnValuePolicy>
</ArchitecturalLevels>
```

Register the root configuration from a `Directory.Build.props` at the solution root so each project hands it to Roslyn:

```xml
<Project>
  <ItemGroup>
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)Architecture.anl" />
  </ItemGroup>
</Project>
```

Roslyn analyzers receive project inputs, not permission to search the solution filesystem. Each project therefore needs this shared `AdditionalFiles` registration. Global policies run before layer policies, so a layer policy may add a stricter rule but cannot relax a global one.

#### Supported direct return matchers

| Child element | Matches | Typical use |
|---|---|---|
| `<Literal>` | A direct literal, including `null`, `""`, numeric values, booleans, and enum casts | `<Literal value="null" />`, `<Literal value="0" />` |
| `<Invocation>` | A direct method invocation | `<Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" />` |
| `<New>` | A direct `new` / target-typed `new()` result | Forbid returning a raw mutable implementation |
| `<Identifier>` | A bare named expression | Forbid a known sentinel name, or allow only named return hand-offs inside `<AllowedReturn>` |
| `<MemberAccess>` | A directly returned property or field access | Forbid a static `None` / `Empty` member where appropriate |

`Literal` has a dedicated `value` attribute. It deliberately supports an empty value, so `<Literal value="" />` means an empty string. Numeric enum casts are unwrapped before matching, so `<Literal value="0" />` also catches `return (PizzaStatus)0;`.

The usual matcher attributes also work where Roslyn can resolve the expression: `typeName`, `exactName`, `exactFullName`, `endsWith`, `startsWith`, `contains`, `regex`, `inherits`, `implements`, `withAttribute`, `withAccessModifier`, and `typeKind`. For example, the annotation matcher above uses the invoked method symbol’s attribute name. That remains configuration-driven: AnaalIJzer does not reference `JetBrains.Annotations`.

The analyzer only rejects values returned **unchanged**. A handling expression such as `lookup.FindPizza() ?? Pizza.Margherita` is not a direct `Invocation` return, because the kitchen has made an explicit fallback decision.

Return-value policies are cumulative through nested layers. An outer policy applies to a child layer, and a child cannot cancel an outer forbidden expression.

There is intentionally no code fix for `ARCH_RET_001`: the configuration identifies an unacceptable result, but only the application can decide the correct replacement.

**Focused examples:**

- [`Example.Arch_RET_001.ExplicitNullReturn`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.ExplicitNullReturn) - `Literal value="null"` rejects a direct null return.
- [`Example.Arch_RET_001.AnnotatedInvocationReturn`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.AnnotatedInvocationReturn) - an annotation matcher rejects returning an optional lookup unchanged.
- [`Example.Arch_RET_001.ConfiguredLiteralReturns`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.ConfiguredLiteralReturns) - empty-string, numeric, and enum-zero sentinels are configuration values.
- [`Example.Arch_RET_001.DirectInvocationReturn`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.DirectInvocationReturn) - `<Invocation />` rejects direct calls while leaving `false`, `null`, and other unrelated return shapes alone.
- [`Example.GlobalReturnValuePolicy`](../../Examples/Features/Example.GlobalReturnValuePolicy) - a wildcard-included, root-level policy applies to unlayered code.
