# Assembly attribute policy from the project file

The SDK turns each `<InternalsVisibleTo Include="..." />` item into a compiled `InternalsVisibleToAttribute`.

- `AllowedExample` is allowed.
- `NotAllowedExample` produces `ARCH024`.

That result is deliberately identical to the source-attribute example: the policy evaluates the attributes emitted by the compilation, so project-file generated and handwritten attributes follow the same rule.

```cmd
dotnet build Examples\Diagnostics\Example.Arch024.AssemblyAttributePolicy.Project -c Release
```
