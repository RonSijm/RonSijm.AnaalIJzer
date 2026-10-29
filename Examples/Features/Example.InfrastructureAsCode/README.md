# Infrastructure as code

This example writes an IJzer configuration in C# instead of maintaining a larger XML document by hand.

- `nameof(Customer)` keeps layer names and matcher names attached to the code being described.
- The object model follows the same structure as the `.anl` XML language.
- `Save` exports ordinary XML. The analyzer does not need the IaC package at runtime.

Generate the committed example file:

```cmd
dotnet run --project Example.InfrastructureAsCode.csproj -- Architecture.anl
```

The generated [`Architecture.anl`](Architecture.anl) can be inspected, committed, included from another configuration, or supplied to the analyzer like any handwritten `.anl` file.
