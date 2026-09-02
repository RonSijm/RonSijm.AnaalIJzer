; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ARCH006 | Architecture | Error | Invalid architecture configuration
ARCH007 | Architecture | Error | Cyclic allowed-dependency graph while enforceAcyclic is enabled
ARCH008 | Architecture | Error | Layer-scoped value-movement or declaration/type name rule violation
ARCH009 | Architecture | Error | Externally visible declaration exposes a type rejected by a layer-scoped API surface policy
ARCH010 | Architecture | Error | A ProjectArchitecture policy rejected a direct MSBuild project reference
ARCH011 | Architecture | Error | A ProjectArchitecture PackagePolicy rejected a resolved NuGet package reference
ARCH012 | Architecture | Error | Layer-scoped declaration accessibility policy violation
ARCH013 | Architecture | Error | A layer-scoped ContractPolicy rejected the declaration shape of a contract type or member
ARCH014 | Architecture | Error | Externally visible declaration transitively exposes a type rejected by a layer-scoped API surface policy
ARCH015 | Architecture | Error | A layer-scoped SourceLocations policy rejected the physical file location of a declaration
ARCH016 | Architecture | Error | A boundary EntryPoints policy rejected an otherwise allowed external dependency
ARCH017 | Architecture | Warning | A configured architecture exception is invalid, expired, or nearing expiry and requires review
ARCH018 | Architecture | Error | Observed source dependencies form a cycle between configured layers
ARCH019 | Architecture | Error | A layer-scoped InheritancePolicy rejected a type that does not inherit or implement the required contract
ARCH020 | Architecture | Error | A layer-scoped ReturnValuePolicy rejected a configured direct method return
