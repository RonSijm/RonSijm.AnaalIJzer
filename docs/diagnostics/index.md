## Diagnostics

The analyzer ships with seven diagnostic IDs. The three dependency-direction rules (ARCH001/004/005) are split by the reason a dependency is illegal, while ARCH006 and ARCH007 protect the integrity of the configuration itself. Dependency diagnostics expose their syntactic site through the `Site` property.

| ID      | Meaning                                                      |
|---------|--------------------------------------------------------------|
| ARCH001 | Illegal layer dependency - no `<AllowedDependency>` edge permits this site |
| ARCH002 | Dependency is unrecognized at a required site                |
| ARCH003 | Type violates an applicable `<Allowed>` or `<Forbidden>` policy |
| ARCH004 | Wrong-direction dependency - reverse of a configured edge    |
| ARCH005 | Same-layer dependency                                        |
| ARCH006 | Invalid architecture configuration                           |
| ARCH007 | Cyclic allowed-dependency graph while `enforceAcyclic` is enabled |

The example projects referenced inline below are self-contained and deliberately broken so Visual Studio, Rider and `dotnet build` show the corresponding `ARCH00X` error.

![Examples in Visual Studio](../../Examples/Assets/Examples-VS-Result.png)
