# Global return-value policy

`Architecture.anl` imports every `.anl` file in `Rules`. The imported policy sits directly under `<ArchitecturalLevels>`, so it applies to all methods in the project without assigning code to a `<Layer>`.

`ServeOvenCallDirectly` returns an invocation directly and raises `ARCH_RET_001`. `ServePreparedPizza` first stores the pizza in `result`, then returns it.

The rule is deliberately a forbidden `<Invocation />` matcher instead of an `<AllowedReturn>` allow-list. `return false;` and `return null;` therefore remain valid. Those expressions need their own configured `<Literal>` rules if a project wants to reject them.

Build the example with:

```powershell
dotnet build Examples\Features\Example.GlobalReturnValuePolicy -c Release
```
