# ARCH_RET_001: Configured Literal Returns

`ReturnValuePolicy` is deliberately generic. Here it blocks direct empty-string, `42`, and enum-zero returns with three `<Literal value="..." />` matchers. The values are configuration, not hard-coded analyzer opinions.

```cmd
dotnet build Examples\Diagnostics\RET\Example.Arch_RET_001.ConfiguredLiteralReturns -c Release
```
