# RonSijm.IJzer.IaC

This package lets you write an IJzer configuration in C# and export it as an ordinary `.anl` file.

```csharp
using RonSijm.IJzer.IaC;

var configuration = new ArchitectureConfiguration
{
    Elements =
    {
        new Layer(nameof(Waiter))
        {
            Elements = { new ClassMatcher { EndsWith = nameof(Waiter) } }
        },
        new Layer(nameof(Chef))
        {
            Elements = { new ClassMatcher { EndsWith = nameof(Chef) } }
        },
        new AllowedDependency(nameof(Waiter), nameof(Chef))
    }
};

configuration.Save("Architecture.anl");
```

- The object model follows the same structure and terminology as the XML language.
- Enum properties cover the closed lists such as dependency sites, declaration targets, and type kinds.
- Collections preserve insertion order, so the generated configuration remains predictable.
- `ToXml()` returns the generated XML when you want to inspect or transform it in memory.
- `Save()` writes UTF-8 without a byte-order mark and can add an XSD location hint.

The generated file is the analyzer input. The analyzed application does not need a runtime reference to this package.

For most repositories I would generate and commit the `.anl` file. That keeps normal IDE and CI builds independent from the generator while still giving the configuration author C# names, types, and refactoring support.
