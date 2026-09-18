# ARCH_RET_001: Explicit Null Return

`PizzaKitchen` has a `ReturnValuePolicy` with `<Literal value="null" />`. Returning `null` directly is reported as `ARCH_RET_001`, while returning a real `Pizza` is allowed. `null` is only one configurable forbidden return value; the policy can also match empty strings, numeric or enum sentinels, direct member access, object creation, or annotated invocations.

```cmd
dotnet build Examples\Diagnostics\RET\Example.Arch_RET_001.ExplicitNullReturn -c Release
```
