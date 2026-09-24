### ARCH_RET_001 - Return-value policy violation

Reported when a method has an applicable global or layer-scoped `<ReturnValuePolicy>` and returns a direct expression rejected by either a forbidden matcher or an `<AllowedReturn>` shape allow-list. A global policy also applies when the containing type is not assigned to a layer.

Example:

```text
'PrepareMysteryPizza' (layer Kitchen) violates return-value policy at MethodReturn:
the ReturnValuePolicy in layer 'Kitchen' blocks returned literal value="null"
```

The diagnostic is reported on the return expression. It can cover `null`, an empty string, a numeric or enum sentinel, a specific member access, object creation, a direct call selected by semantic matcher attributes, or any shape omitted from an `<AllowedReturn>` block.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `Site` (`MethodReturn`)
- `ReturnValueRuleTarget`
- `ReturnValueRule`
- `ReturnValueRuleMode` (`Forbidden` for a matching forbidden child, `Allowed` for a value omitted from an allow-list)
- `ViolationReason`
- the originating rule path, line, and column

Typical fixes:

- return a meaningful value rather than the configured sentinel;
- turn an optional lookup into an explicit fallback or error result before returning it;
- assign a direct invocation to a named result before returning it when the policy requires `<AllowedReturn><Identifier /></AllowedReturn>`;
- move the method outside the layer only when the policy is layer-scoped; a global policy intentionally follows every method;
- narrow the policy only when that direct return is intentionally allowed.

There is no automatic code fix. The policy tells AnaalIJzer which return expression is unacceptable; it cannot know which domain-specific value, result type, variable name, fallback, or exception behavior is correct. The analyzer recognises the rejected shape; it has no opinion about what your domain should say instead.

**Focused examples:** [`Example.Arch_RET_001.ExplicitNullReturn`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.ExplicitNullReturn), [`Example.Arch_RET_001.AnnotatedInvocationReturn`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.AnnotatedInvocationReturn), [`Example.Arch_RET_001.ConfiguredLiteralReturns`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.ConfiguredLiteralReturns), [`Example.Arch_RET_001.DirectInvocationReturn`](../../Examples/Diagnostics/RET/Example.Arch_RET_001.DirectInvocationReturn), and [`Example.GlobalReturnValuePolicy`](../../Examples/Features/Example.GlobalReturnValuePolicy).

#### Real-world uses

- Stop a service layer from returning `null`, `string.Empty`, zero, or a known enum sentinel as an undeclared “not found” signal.
- Require a nullable third-party call to become a domain fallback, `Result`, or other explicit outcome before it escapes a selected boundary.
- Require a method to return through a named hand-off point so the layer has a natural place for inspection, logging, normalization, or a later handling rule.
