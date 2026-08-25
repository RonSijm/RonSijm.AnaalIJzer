# Boundary entry points

This example shows a parent boundary that allows outside callers into `Ordering` only through `Ordering/Contracts`.

- `CandyController -> PlaceCandyContract` is valid.
- `CandyAdminController -> CandyOrderingService` produces `ARCH016`.
- `CandyOrderingService -> PlaceCandyContract` is internal to `Ordering`, so the entry-point policy does not apply.

Build in Release to run the analyzer:

```cmd
dotnet build Examples\Diagnostics\Example.Arch016.BoundaryEntryPoints -c Release
```
