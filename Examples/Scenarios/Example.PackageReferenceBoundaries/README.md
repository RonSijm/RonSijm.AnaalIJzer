# Example.PackageReferenceBoundaries

This scenario shows project-level package policies. `Example.PackageReferenceBoundaries.Domain` directly references `Microsoft.Extensions.Logging` and gets `ARCH011`. `Example.PackageReferenceBoundaries.Data` references the same package but is allowed by a scoped `PackagePolicy`.

The point is that package boundaries are checked from resolved project/package topology, not from whether some type from that package happens to be used in source code yet.
