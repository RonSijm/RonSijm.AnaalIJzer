### Return-value policies

`<ReturnValuePolicy>` rejects configured **direct return expressions** from methods in its owning layer and descendants. It is useful when a particular return value is a sentinel that hides a decision the method should make explicitly.

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

Sibling matcher elements are alternatives: returning a value matching **any** one produces `ARCH020`. Attributes on one matcher are combined, just like layer matchers.

#### Supported direct return matchers

| Child element | Matches | Typical use |
|---|---|---|
| `<Literal>` | A direct literal, including `null`, `""`, numeric values, booleans, and enum casts | `<Literal value="null" />`, `<Literal value="0" />` |
| `<Invocation>` | A direct method invocation | `<Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" />` |
| `<New>` | A direct `new` / target-typed `new()` result | Forbid returning a raw mutable implementation |
| `<Identifier>` | A directly returned identifier | Forbid returning a known sentinel variable |
| `<MemberAccess>` | A directly returned property or field access | Forbid a static `None` / `Empty` member where appropriate |

`Literal` has a dedicated `value` attribute. It deliberately supports an empty value, so `<Literal value="" />` means an empty string. Numeric enum casts are unwrapped before matching, so `<Literal value="0" />` also catches `return (PizzaStatus)0;`.

The usual matcher attributes also work where Roslyn can resolve the expression: `typeName`, `exactName`, `exactFullName`, `endsWith`, `startsWith`, `contains`, `regex`, `inherits`, `implements`, `withAttribute`, `withAccessModifier`, and `typeKind`. For example, the annotation matcher above uses the invoked method symbol’s attribute name. That remains configuration-driven: AnaalIJzer does not reference `JetBrains.Annotations`.

The analyzer only rejects values returned **unchanged**. A handling expression such as `lookup.FindPizza() ?? Pizza.Margherita` is not a direct `Invocation` return, because the kitchen has made an explicit fallback decision.

Return-value policies are cumulative through nested layers. An outer policy applies to a child layer, and a child cannot cancel an outer forbidden expression.

There is intentionally no code fix for `ARCH020`: the configuration identifies an unacceptable result, but only the application can decide the correct replacement.

**Focused examples:**

- [`Example.Arch020.ExplicitNullReturn`](../../Examples/Diagnostics/Example.Arch020.ExplicitNullReturn) - `Literal value="null"` rejects a direct null return.
- [`Example.Arch020.AnnotatedInvocationReturn`](../../Examples/Diagnostics/Example.Arch020.AnnotatedInvocationReturn) - an annotation matcher rejects returning an optional lookup unchanged.
- [`Example.Arch020.ConfiguredLiteralReturns`](../../Examples/Diagnostics/Example.Arch020.ConfiguredLiteralReturns) - empty-string, numeric, and enum-zero sentinels are configuration values.
