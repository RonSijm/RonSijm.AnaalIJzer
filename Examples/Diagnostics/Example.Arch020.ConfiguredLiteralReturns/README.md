# ARCH020: Configured Literal Returns

`ReturnValuePolicy` is deliberately generic. Here it blocks direct empty-string, `42`, and enum-zero returns with three `<Literal value="..." />` matchers. The values are configuration, not hard-coded analyzer opinions.

```cmd
dotnet build Examples\Diagnostics\Example.Arch020.ConfiguredLiteralReturns -c Release
```
