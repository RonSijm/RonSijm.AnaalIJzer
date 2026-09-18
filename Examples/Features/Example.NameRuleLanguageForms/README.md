# Example.NameRuleLanguageForms

This one-file example keeps `RequireMatchingNames` in its default `Direct` mode and shows eight `ARCH_NAME_008` diagnostics across direct C# forms: compound assignment, deconstruction, both conditional branches after conversion, an expression-bodied return, and named arguments.

```cmd
dotnet build Examples\Features\Example.NameRuleLanguageForms -c Release
```

The deliberately broken lines show that syntax changes do not erase a value name's meaning. It does not follow a value through a neutral local alias; see `Example.NameRuleIntraProceduralTracking` for that opt-in behavior.
