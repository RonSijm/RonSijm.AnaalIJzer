## Diagnostics

The analyzer ships with twenty-nine compiler diagnostic IDs. IDs follow `ARCH_<CONCERN>_<REASON>`: the concern names the policy family and the shared three-digit reason identifies the kind of failure. Dependency, name-rule, API-surface, return-value, operation-policy, and namespace-hierarchy diagnostics expose their syntactic site through the `Site` property where applicable.

| ID      | Meaning                                                      |
|---------|--------------------------------------------------------------|
| ARCH_DEP_001 | Illegal layer dependency - no `<AllowedDependency>` edge permits this site |
| ARCH_DEP_002 | Dependency is unrecognized at a required site                |
| ARCH_TYPE_001 | Type violates an applicable `<Allowed>` or `<Forbidden>` policy |
| ARCH_DEP_004 | Wrong-direction dependency - reverse of a configured edge    |
| ARCH_DEP_005 | Same-layer dependency                                        |
| ARCH_CONF_003 | Invalid architecture configuration                           |
| ARCH_CONF_006 | Cyclic allowed-dependency graph while `enforceAcyclic` is enabled |
| ARCH_NAME_008 | Name rule violation                                          |
| ARCH_API_001 | Externally visible API exposes a type rejected by its layer policy |
| ARCH_PROJ_001 | Direct project reference violates `ProjectArchitecture`      |
| ARCH_PKG_001 | Direct package reference violates `ProjectArchitecture`      |
| ARCH_VIS_001 | Declared accessibility violates a layer visibility policy    |
| ARCH_CONT_008 | Contract declaration shape violates a layer contract policy  |
| ARCH_API_010 | Allowed API root transitively exposes a type rejected by its layer policy |
| ARCH_SRC_007 | Layer source declaration is outside an allowed source location |
| ARCH_BOUND_007 | Dependency enters a boundary through a disallowed entry point |
| ARCH_EXC_009 | Architecture exception metadata, expiry, or stale state requires review |
| ARCH_DEP_006 | Observed source dependencies form a cycle between configured layers |
| ARCH_INH_001 | Declared base type or implemented interfaces violate a layer inheritance policy |
| ARCH_RET_001 | A direct returned expression violates a layer return-value policy |
| ARCH_OPER_001 | A selected resolved operation violates a layer forbidden-operation policy |
| ARCH_OPER_002 | A required operation is absent or does not dominate every exit |
| ARCH_OPER_011 | A selected operation exceeds its configured maximum count |
| ARCH_OPER_012 | Selected operations violate configured ordering |
| ARCH_OPCT_001 | An operation-contract participant belongs to a disallowed layer |
| ARCH_OPCT_002 | A required request or owner invocation is missing |
| ARCH_OPCT_008 | An operation-contract response shape does not match |
| ARCH_ASSM_001 | A compiled assembly attribute violates an `AssemblyAttributePolicy` |
| ARCH_NS_007 | A source namespace relationship violates a `NamespaceHierarchyPolicy` |

The example projects referenced inline below are self-contained and deliberately broken so Visual Studio, Rider and `dotnet build` show the corresponding `ARCH_<CONCERN>_<REASON>` error. They fail on purpose; the repository is not having a bad day.

![Examples in Visual Studio](../../Examples/Assets/Examples-VS-Result.png)
