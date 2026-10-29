; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.0.3

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ARCH001 | Architecture | Error | Illegal architectural layer dependency (no AllowedDependency edge configured)
ARCH002 | Architecture | Error | Unrecognized dependency at a site configured by requireRecognizedDependencies
ARCH003 | Architecture | Error | Forbidden architectural dependency
ARCH004 | Architecture | Error | Wrong-direction architectural dependency (reverse edge is configured)
ARCH005 | Architecture | Error | Same-layer architectural dependency
