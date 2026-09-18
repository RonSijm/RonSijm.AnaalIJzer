# Assembly attribute policy from source

This project checks the final semantic assembly attributes, not a particular C# spelling.

- `[assembly: InternalsVisibleTo("AllowedExample")]` is allowed.
- `[assembly: InternalsVisibleTo("NotAllowedExample")]` produces `ARCH_ASSM_001`.

The policy matches the full attribute type and the first constructor argument. It does not make AnaalIjzer depend on `System.Runtime.CompilerServices`; that namespace only appears here because the sample itself declares the framework attribute.

```cmd
dotnet build Examples\Diagnostics\ASSM\Example.Arch_ASSM_001.AssemblyAttributePolicy.Code -c Release
```
