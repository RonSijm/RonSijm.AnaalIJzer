## IDE code fixes

Visual Studio and Rider can now apply AnaalIJzer fixes against both:

- file-based `Architecture.anl`;
- inline `AssemblyMetadata("AnaalIJzerSettings", ...)`.

The analyzer still owns the diagnostics. The code-fix layer only proposes deterministic edits that are local, previewable, and unlikely to invent architecture by accident. Silently widening a boundary because a build went red is how a layering rule turns into a layering suggestion.

There are two families of fixes:

- **source fixes** change C# code directly;
- **configuration fixes** change `Architecture.anl` or inline `AssemblyMetadata`.

Arse reuses the same configuration-fix catalog headlessly through `arse fixes` and `arse apply-fix`. That means the light-bulb suggestions you see in the IDE and the proposal list you can review in CI or a terminal come from the same underlying fix implementations.

The Visual Studio dependency-graph tool window now reuses that same shared catalog as well. From `Extensions > IJzer > Show Dependency Graphs`, the graph can load fix proposals for the active project, preview the config diff, and apply one proposal without leaving the graph view. The root inspector shows the full project list, while selecting or right-clicking a layer or dependency connection switches to a filtered selection-scoped view.

For the broader mental model, ownership rules, and risk labels, see [Configuration fixers](config-fixers.md).

### Support matrix

| Diagnostic | IDE fix support | Covered by tests |
|---|---|---|
| `ARCH_DEP_001` | add missing `<AllowedDependency>`; extend `allowedSites`; relax `blockedSites`; add exception | `DependencyRuleCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH_DEP_002` | classify the unknown dependency into an existing layer; remove the current site from `requireRecognizedDependencies` globally or for the current caller layer | `RecognizedDependencyCodeFixTests.cs` |
| `ARCH_TYPE_001` | forbidden rule match: rename via `<Fix Rename="...">` or add exception; allow-list failure: add exact `<Class typeName="..."/>` to every applicable `<Allowed>` list | `RenameCodeFixTests.cs`, `AllowedTypePolicyCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH_DEP_004` | add the forward `<AllowedDependency>`; flip the exact configured reverse `<AllowedDependency>` when one concrete reverse rule exists; repair site filters; add exception | `DependencyRuleCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH_DEP_005` | add a same-layer self-edge, optionally site-scoped; add exception | `DependencyRuleCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH_CONF_006` | for each concrete allowed edge in a configured cycle: add a matching blocking edge, or remove that allowed edge; the user chooses the edge | `CycleDependencyCodeFixTests.cs` |
| `ARCH_NAME_008` | rename the declaration when the rule compares declaration name to semantic type; add `<Allow from="..." to="..."/>` mappings, including a site-scoped variant for `RequireMatchingNames` | `DeclarationNameCodeFixTests.cs`, `NameRuleAllowMappingCodeFixTests.cs` |
| `ARCH_API_001` | add or widen `<ApiSurface><AllowedLayer ... /></ApiSurface>`; relax `blockedSites`; disable `requireRecognizedTypes` when that is the denial | `ApiSurfacePolicyCodeFixTests.cs` |
| `ARCH_PROJ_001` | add a missing group-level `<AllowedProjectReference>`; add a narrow exact-project rule with `<From>` and `<To>` selectors; add an explicit same-group self-edge; remove the matching blocking `<BlockedProjectReference>` rule | `ProjectArchitectureCodeFixTests.cs` |
| `ARCH_PKG_001` | append an exact `<Package exactName="..."/>` matcher to the matched allowed package list | `PackagePolicyCodeFixTests.cs` |
| `ARCH_VIS_001` | add the reported visibility to `allowedAccessibilities`; remove it from `blockedAccessibilities`; remove a single-value blocking policy entirely | `VisibilityPolicyCodeFixTests.cs` |
| `ARCH_CONT_008` | remove a disallowed property setter when the violation is exactly that accessor | `ContractPurityCodeFixTests.cs` |
| `ARCH_API_010` | the same `ApiSurface` configuration fixes as `ARCH_API_001` | `ApiSurfacePolicyCodeFixTests.cs` |
| `ARCH_SRC_007` | add an exact `<Source exactName="..."/>` rule to the owning layer | `SourceLocationCodeFixTests.cs` |
| `ARCH_BOUND_007` | add a boundary `<EntryPoint>`; add a required site to `allowedSites`; remove the current site from `blockedSites` | `BoundaryEntryPointCodeFixTests.cs` |
| `ARCH_DEP_006` | no configuration fix: this reports an observed source-code cycle, which configuration editing cannot honestly repair | `ExampleConfigurationFixIntegrationTests.cs` |
| `ARCH_INH_001` | add a single required base type or a single required interface when the change is unambiguous | `InheritancePolicyCodeFixTests.cs` |
| `ARCH_RET_001` | no automatic fix: a rejected return expression does not tell the analyzer which domain result, named hand-off, or fallback should replace it | `ReturnValuePolicyAnalyzerTests.cs` |
| `ARCH_OPER_001` | no automatic fix: a forbidden selected operation does not identify the intended adapter, async flow, or composition-boundary change | `ForbiddenOperationPolicyAnalyzerTests.cs` |
| `ARCH_OPER_002`, `ARCH_OPER_011`, `ARCH_OPER_012` | no automatic fix: adding, moving, or removing an operation requires an explicit workflow decision | `BehavioralOperationPolicyAnalyzerTests.cs` |
| `ARCH_OPCT_001`, `ARCH_OPCT_002`, `ARCH_OPCT_008` | no automatic fix: choosing an operation owner, entry-point delegation, or contract shape requires an explicit workflow decision | `OperationContractAnalyzerTests.cs` |
| `ARCH_ASSM_001` | no automatic fix: changing emitted assembly metadata or widening its allow/deny policy requires an explicit ownership decision | `AssemblyAttributePolicyCodeFixTests.cs` |
| `ARCH_NS_007` | no automatic fix: moving namespace ownership, introducing a contract, or changing a namespace relationship requires an explicit architectural decision | `NamespaceHierarchyPolicyAnalyzerTests.cs` |

### Deliberate limits

- `ARCH_PROJ_001` and `ARCH_PKG_001` are compilation-end diagnostics. The config edits exist and are covered by analyzer tests, but whether an IDE host shows them as ordinary editor light bulbs depends on how that host surfaces `Location.None` diagnostics.
- `ARCH_CONT_008`, `ARCH_INH_001`, `ARCH_RET_001`, `ARCH_OPER_*`, `ARCH_OPCT_*`, `ARCH_ASSM_001`, and `ARCH_NS_007` stay intentionally narrow. If the analyzer cannot tell which one deterministic edit is the right one, it does not guess. A confidently wrong automatic fix is harder to spot in review than no fix at all.
- Configuration fixers preserve the owning source where possible:
  - if a rule came from an included `.anl`, that included file is edited;
  - if the config came from inline `AssemblyMetadata`, the source file containing the assembly attribute is rewritten.

For a light-bulb-friendly baseline, start with the analyzer tests in `src/Tests/RonSijm.AnaalIJzer.Analyzer.Tests/Diagnostics/`.

For the end-to-end headless path, see `src/Tests/RonSijm.AnaalIJzer.Application.Tests/ApplicationOperations/ApplicationOperationsTests.ConfigurationFixes.cs`.

For real example-project coverage, including expected proposal titles for included `.anl`, inline `AssemblyMetadata`, site filters, name rules, source locations, and project architecture scenarios, see `src/Tests/RonSijm.AnaalIJzer.IntegrationTests/ExampleConfigurationFixIntegrationTests.cs`.

For Visual Studio graph-window state coverage, including preserving the active project context across graph refreshes, see `src/Tests/RonSijm.AnaalIJzer.VisualStudio.Tests/Graphs/ArchitectureGraphToolWindowStateTests.cs`.
