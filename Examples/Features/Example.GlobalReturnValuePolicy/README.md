# Global return-value policy

`Architecture.anl` imports every `.anl` file in `Rules`. The imported policy sits directly under `<ArchitecturalLevels>`, so it applies to all methods in the project without assigning code to a `<Layer>`.

`ServePizzaWithoutANamedResult` returns an invocation directly and raises `ARCH_RET_001`. `ServePizzaWithANamedResult` first stores the pizza in `result`, then returns the identifier, which the rule permits.

Build the example with:

```powershell
dotnet build Examples\Features\Example.GlobalReturnValuePolicy -c Release
```
