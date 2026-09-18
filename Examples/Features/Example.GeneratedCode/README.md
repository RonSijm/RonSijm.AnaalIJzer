# Generated code analysis

Generated source is quiet by default because generated files are often large, volatile, and owned by another tool. This example opts in only `Generated_Clock_Kitchen.g.cs`, which deliberately reads `DateTime.UtcNow` and raises one `ARCH_OPER_001`.

`PizzaKitchen.cs` shows that ordinary source remains analyzed without any generated-code setting. `Generated_Menu_Kitchen.g.cs` would remain ignored because it does not match the configured `<Path>`.

```cmd
dotnet build Examples\Features\Example.GeneratedCode -c Release
```
