## IDE code fixes

Visual Studio and Rider can now apply AnaalIJzer fixes against both:

- file-based `Architecture.anl`;
- inline `AssemblyMetadata("AnaalIJzerSettings", ...)`.

The analyzer still owns the diagnostics. The code-fix layer only proposes deterministic edits that are local, previewable, and unlikely to invent architecture by accident.

There are two families of fixes:

- **source fixes** change C# code directly;
- **configuration fixes** change `Architecture.anl` or inline `AssemblyMetadata`.

Arse reuses the same configuration-fix catalog headlessly through `arse fixes` and `arse apply-fix`. That means the light-bulb suggestions you see in the IDE and the proposal list you can review in CI or a terminal come from the same underlying fix implementations.

The Visual Studio dependency-graph tool window now reuses that same shared catalog as well. From `Extensions > IJzer > Show Dependency Graphs`, the graph can load fix proposals for the active project, preview the config diff, and apply one proposal without leaving the graph view. The root inspector shows the full project list, while selecting or right-clicking a layer or dependency connection switches to a filtered selection-scoped view.

For the broader mental model, ownership rules, and risk labels, see [Configuration fixers](config-fixers.md).

### Support matrix

| Diagnostic | IDE fix support | Covered by tests |
|---|---|---|
| `ARCH001` | add missing `<AllowedDependency>`; extend `allowedSites`; relax `blockedSites`; add exception | `DependencyRuleCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH002` | classify the unknown dependency into an existing layer; remove the current site from `requireRecognizedDependencies` globally or for the current caller layer | `RecognizedDependencyCodeFixTests.cs` |
| `ARCH003` | forbidden rule match: rename via `<Fix Rename="...">` or add exception; allow-list failure: add exact `<Class typeName="..."/>` to every applicable `<Allowed>` list | `RenameCodeFixTests.cs`, `AllowedTypePolicyCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH004` | add the forward `<AllowedDependency>`; flip the exact configured reverse `<AllowedDependency>` when one concrete reverse rule exists; repair site filters; add exception | `DependencyRuleCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH005` | add a same-layer self-edge, optionally site-scoped; add exception | `DependencyRuleCodeFixTests.cs`, `AddToExceptionsCodeFixTests.cs` |
| `ARCH007` | for each concrete allowed edge in a configured cycle: add a matching blocking edge, or remove that allowed edge; the user chooses the edge | `CycleDependencyCodeFixTests.cs` |
| `ARCH008` | rename the declaration when the rule compares declaration name to semantic type; add `<Allow from="..." to="..."/>` mappings, including a site-scoped variant for `RequireMatchingNames` | `DeclarationNameCodeFixTests.cs`, `NameRuleAllowMappingCodeFixTests.cs` |
| `ARCH009` | add or widen `<ApiSurface><AllowedLayer ... /></ApiSurface>`; relax `blockedSites`; disable `requireRecognizedTypes` when that is the denial | `ApiSurfacePolicyCodeFixTests.cs` |
| `ARCH010` | add a missing `<AllowedProjectReference>`; add an explicit same-group self-edge; remove the matching blocking `<BlockedProjectReference>` rule | `ProjectArchitectureCodeFixTests.cs` |
| `ARCH011` | append an exact `<Package exactName="..."/>` matcher to the matched allowed package list | `PackagePolicyCodeFixTests.cs` |
| `ARCH012` | add the reported visibility to `allowedAccessibilities`; remove it from `blockedAccessibilities`; remove a single-value blocking policy entirely | `VisibilityPolicyCodeFixTests.cs` |
| `ARCH013` | remove a disallowed property setter when the violation is exactly that accessor | `ContractPurityCodeFixTests.cs` |
| `ARCH014` | the same `ApiSurface` configuration fixes as `ARCH009` | `ApiSurfacePolicyCodeFixTests.cs` |
| `ARCH015` | add an exact `<Source exactName="..."/>` rule to the owning layer | `SourceLocationCodeFixTests.cs` |
| `ARCH016` | add a boundary `<EntryPoint>`; add a required site to `allowedSites`; remove the current site from `blockedSites` | `BoundaryEntryPointCodeFixTests.cs` |
| `ARCH018` | no configuration fix: this reports an observed source-code cycle, which configuration editing cannot honestly repair | `ExampleConfigurationFixIntegrationTests.cs` |
| `ARCH019` | add a single required base type or a single required interface when the change is unambiguous | `InheritancePolicyCodeFixTests.cs` |
| `ARCH020` | no automatic fix: a forbidden return expression does not tell the analyzer which domain result should replace it | `ReturnValuePolicyAnalyzerTests.cs` |

### Deliberate limits

- `ARCH010` and `ARCH011` are compilation-end diagnostics. The config edits exist and are covered by analyzer tests, but whether an IDE host shows them as ordinary editor light bulbs depends on how that host surfaces `Location.None` diagnostics.
- `ARCH013`, `ARCH019`, and `ARCH020` stay intentionally narrow. If the analyzer cannot tell which one deterministic edit is the right one, it does not guess.
- Configuration fixers preserve the owning source where possible:
  - if a rule came from an included `.anl`, that included file is edited;
  - if the config came from inline `AssemblyMetadata`, the source file containing the assembly attribute is rewritten.

For a light-bulb-friendly baseline, start with the analyzer tests in `src/Tests/RonSijm.AnaalIJzer.Analyzer.Tests/Diagnostics/`.

For the end-to-end headless path, see `src/Tests/RonSijm.AnaalIJzer.Application.Tests/ApplicationOperations/ApplicationOperationsTests.ConfigurationFixes.cs`.

For real example-project coverage, including expected proposal titles for included `.anl`, inline `AssemblyMetadata`, site filters, name rules, source locations, and project architecture scenarios, see `src/Tests/RonSijm.AnaalIJzer.IntegrationTests/ExampleConfigurationFixIntegrationTests.cs`.

For Visual Studio graph-window state coverage, including preserving the active project context across graph refreshes, see `src/Tests/RonSijm.AnaalIJzer.VisualStudio.Tests/Graphs/ArchitectureGraphToolWindowStateTests.cs`.
