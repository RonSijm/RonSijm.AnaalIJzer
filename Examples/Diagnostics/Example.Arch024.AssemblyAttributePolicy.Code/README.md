# Assembly attribute policy from source

This project checks the final semantic assembly attributes, not a particular C# spelling.

- `[assembly: InternalsVisibleTo("AllowedExample")]` is allowed.
- `[assembly: InternalsVisibleTo("NotAllowedExample")]` produces `ARCH024`.

The policy matches the full attribute type and the first constructor argument. It does not make AnaalIjzer depend on `System.Runtime.CompilerServices`; that namespace only appears here because the sample itself declares the framework attribute.

```cmd
dotnet build Examples\Diagnostics\Example.Arch024.AssemblyAttributePolicy.Code -c Release
```
