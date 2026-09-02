# ARCH020: Explicit Null Return

`PizzaKitchen` has a `ReturnValuePolicy` with `<Literal value="null" />`. Returning `null` directly is reported as `ARCH020`, while returning a real `Pizza` is allowed. `null` is only one configurable forbidden return value; the policy can also match empty strings, numeric or enum sentinels, direct member access, object creation, or annotated invocations.

```cmd
dotnet build Examples\Diagnostics\Example.Arch020.ExplicitNullReturn -c Release
```
