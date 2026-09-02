## Diagnostics

The analyzer ships with twenty diagnostic IDs. The three dependency-direction rules (ARCH001/004/005) are split by the reason a dependency is illegal, while ARCH006 and ARCH007 protect the integrity of the configuration itself. Dependency, name-rule, API-surface, and return-value diagnostics expose their syntactic site through the `Site` property where applicable.

| ID      | Meaning                                                      |
|---------|--------------------------------------------------------------|
| ARCH001 | Illegal layer dependency - no `<AllowedDependency>` edge permits this site |
| ARCH002 | Dependency is unrecognized at a required site                |
| ARCH003 | Type violates an applicable `<Allowed>` or `<Forbidden>` policy |
| ARCH004 | Wrong-direction dependency - reverse of a configured edge    |
| ARCH005 | Same-layer dependency                                        |
| ARCH006 | Invalid architecture configuration                           |
| ARCH007 | Cyclic allowed-dependency graph while `enforceAcyclic` is enabled |
| ARCH008 | Name rule violation                                          |
| ARCH009 | Externally visible API exposes a type rejected by its layer policy |
| ARCH010 | Direct project reference violates `ProjectArchitecture`      |
| ARCH011 | Direct package reference violates `ProjectArchitecture`      |
| ARCH012 | Declared accessibility violates a layer visibility policy    |
| ARCH013 | Contract declaration shape violates a layer contract policy  |
| ARCH014 | Allowed API root transitively exposes a type rejected by its layer policy |
| ARCH015 | Layer source declaration is outside an allowed source location |
| ARCH016 | Dependency enters a boundary through a disallowed entry point |
| ARCH017 | Architecture exception metadata, expiry, or stale state requires review |
| ARCH018 | Observed source dependencies form a cycle between configured layers |
| ARCH019 | Declared base type or implemented interfaces violate a layer inheritance policy |
| ARCH020 | A direct returned expression violates a layer return-value policy |

The example projects referenced inline below are self-contained and deliberately broken so Visual Studio, Rider and `dotnet build` show the corresponding `ARCH00X` error.

![Examples in Visual Studio](../../Examples/Assets/Examples-VS-Result.png)
