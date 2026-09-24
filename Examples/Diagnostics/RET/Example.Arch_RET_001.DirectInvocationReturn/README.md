# ARCH_RET_001: Direct Invocation Return

`PizzaKitchen` uses a forbidden `<Invocation />` return matcher. A direct `return oven.BakePizza();` reports `ARCH_RET_001`; storing the pizza in `result` and returning `result` is allowed.

The rule says "do not return a method invocation directly." It does not say "only return a variable." That distinction matters: `return false;` and `return null;` are valid here because neither expression is an invocation. A project can reject those values separately with configured `<Literal>` matchers.

```cmd
dotnet build Examples\Diagnostics\RET\Example.Arch_RET_001.DirectInvocationReturn -c Release
```
