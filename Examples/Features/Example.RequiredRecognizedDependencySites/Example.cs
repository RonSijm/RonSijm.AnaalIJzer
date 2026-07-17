// ReSharper disable All - Justification: Example File
// requireRecognizedDependencies accepts the same site names as allowedSites and blockedSites.
// This project intentionally produces one ARCH002 diagnostic for every configured site.

namespace Example.RequiredRecognizedDependencySites;

public sealed class KnownDependency { }

// Valid: KnownDependency belongs to the Known layer.
public sealed class RecognizedConstructorCaller(KnownDependency dependency) { }