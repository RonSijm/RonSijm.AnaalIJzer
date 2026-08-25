# Source locations

This example shows that a type can match the correct layer by namespace and still be misplaced on disk.

- `AllowedCandyService` is in namespace `SweetShop.Ordering` and in `Ordering/`, so it passes.
- `MisplacedCandyService` is in the same namespace but lives under `Infrastructure/`, so it raises `ARCH015`.

Build in Release to run the analyzer:

```cmd
dotnet build Examples\Features\Example.SourceLocations -c Release
```
