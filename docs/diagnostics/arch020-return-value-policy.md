### ARCH020 - Return-value policy violation

Reported when a method belongs to a layer with an applicable `<ReturnValuePolicy>` and returns a configured forbidden expression unchanged.

Example:

```text
'PrepareMysteryPizza' (layer Kitchen) violates return-value policy at MethodReturn:
the ReturnValuePolicy in layer 'Kitchen' blocks returned literal value="null"
```

The diagnostic is reported on the return expression. It can cover `null`, an empty string, a numeric or enum sentinel, a specific member access, object creation, or a direct call selected by semantic matcher attributes.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `Site` (`MethodReturn`)
- `ReturnValueRuleTarget`
- `ReturnValueRule`
- `ViolationReason`
- the originating rule path, line, and column

Typical fixes:

- return a meaningful value rather than the configured sentinel;
- turn an optional lookup into an explicit fallback or error result before returning it;
- move the method outside the layer only when that layer policy does not apply to it;
- narrow the policy only when that direct return is intentionally allowed.

There is no automatic code fix. The policy tells AnaalIJzer what must not escape the method; it cannot know which domain-specific value, result type, fallback, or exception behavior is correct.

**Focused examples:** [`Example.Arch020.ExplicitNullReturn`](../../Examples/Diagnostics/Example.Arch020.ExplicitNullReturn), [`Example.Arch020.AnnotatedInvocationReturn`](../../Examples/Diagnostics/Example.Arch020.AnnotatedInvocationReturn), and [`Example.Arch020.ConfiguredLiteralReturns`](../../Examples/Diagnostics/Example.Arch020.ConfiguredLiteralReturns).
