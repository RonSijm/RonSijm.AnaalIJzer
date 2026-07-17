# Examples

This folder contains runnable sample projects for **RonSijm.AnaalIJzer**. Simple one-file examples keep their top-level config inline in `Example.cs` with `AssemblyMetadata("AnaalIJzerSettings", ...)`, so exact type rules can use `nameof(...)` and stay refactor-safe. Broader multi-file, scenario, or include-focused examples use `Architecture.anl`, which keeps larger rule sets easier to scan. Most examples intentionally fail with documented architectural diagnostics, while a few demonstrate clean configuration or generated documentation output.

> The repository root [`README.md`](../README.md) is the primary documentation. This file just explains the build wiring of the `Examples/` folder itself.

---

## The analyzer toggle

Because most diagnostic example projects **deliberately fail to build** (that's the point — they trigger `ARCH00X` errors), they would make opening `RonSijm.AnaalIJzer.WithExamples.slnx` in an IDE intolerable if the analyzer were always on. The wiring in [`Directory.Build.props`](Directory.Build.props) gates it behind configuration:

| Build                                                          | Analyzer | Notes                                                                |
| -------------------------------------------------------------- | -------- | -------------------------------------------------------------------- |
| `dotnet build … -c Debug` *(IDE default)*                      | **OFF**  | Silent. Project loads cleanly, you can browse the source.            |
| `dotnet build … -c Release`                                    | **ON**   | Diagnostic examples report their `ARCH00X` errors. Generated docs are handled by Arse. |
| `dotnet build … -c Debug -p:EnableAnalyzerOnDebug=true`        | **ON**   | One-off Debug run with the analyzer attached.                        |
| `dotnet build … -p:EnableArchitecturalLevelAnalyzer=true`      | **ON**   | Force-on, regardless of configuration.                               |
| `dotnet build … -p:EnableArchitecturalLevelAnalyzer=false`     | **OFF**  | Force-off, regardless of configuration.                              |

### Making the IDE noisy on purpose

If you want to *see* the squiggles while editing — for example, you're working on the analyzer itself and want immediate feedback from the example projects — open [`Directory.Build.props`](Directory.Build.props) and flip:

```xml
<EnableAnalyzerOnDebug Condition="'$(EnableAnalyzerOnDebug)' == ''">false</EnableAnalyzerOnDebug>
```

…to `true`. Reload the solution and the diagnostic examples will start reporting in the Error List.

### Resolution order

`EnableArchitecturalLevelAnalyzer` is the property that actually controls the wiring. It resolves in this order (first match wins):

1. An explicit value set on the command line or inside a `.csproj` (e.g. the unit-test project sets it to `false` so the analyzer source files don't analyze themselves).
2. `Configuration == Release` → `true`.
3. `EnableAnalyzerOnDebug == true` → `true`.
4. Otherwise → `false`.

When `true`, the props file adds the analyzer `ProjectReference` (with `OutputItemType="Analyzer"`) and registers project-local settings files as `AdditionalFiles` so inline configs can still use `<Include>`.

---

## Folder layout

The main [`README.md`](../README.md) explains each feature inline. The folders here are grouped by the kind of behavior they demonstrate:

| Folder | Purpose |
| ------ | ------- |
| [`Diagnostics/`](Diagnostics) | One-project samples for individual `ARCH00X` diagnostics. |
| [`Features/`](Features) | One-project samples for configuration features such as exceptions, includes, inline XML and wildcards. |
| [`Documentation/`](Documentation) | Samples and generated output for reports and architecture documentation. |
| [`Scenarios/`](Scenarios) | Usage-pattern examples, including scenarios that may grow into multiple projects. |
| [`StarterConfigs/`](StarterConfigs) | Copyable reference `Architecture.anl` files. |
| [`Assets/`](Assets) | Images used by the documentation. |

### Diagnostics

| Folder | Main README section |
| ------ | ------------------- |
| [`Example.Arch001.SkipsLayer`](Diagnostics/Example.Arch001.SkipsLayer) | [`ARCH001 - Illegal layer dependency`](../README.md#arch001---illegal-layer-dependency) |
| [`Example.Arch001.NoEdge`](Diagnostics/Example.Arch001.NoEdge) | [`ARCH001 - Illegal layer dependency`](../README.md#arch001---illegal-layer-dependency) |
| [`Example.Arch001.GenericTypeArgument`](Diagnostics/Example.Arch001.GenericTypeArgument) | [`Matcher types`](../README.md#matcher-types) |
| [`Example.Arch001.NonConstructorInjection`](Diagnostics/Example.Arch001.NonConstructorInjection) | [`Diagnostic properties`](../README.md#diagnostic-properties) |
| [`Example.Arch002.UnrecognizedDependency`](Diagnostics/Example.Arch002.UnrecognizedDependency) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.Arch003.ForbiddenType`](Diagnostics/Example.Arch003.ForbiddenType) | [`<Forbidden>`](../README.md#forbidden) |
| [`Example.Arch004.WrongDirection`](Diagnostics/Example.Arch004.WrongDirection) | [`ARCH004 - Wrong-direction dependency`](../README.md#arch004---wrong-direction-dependency) |
| [`Example.Arch005.SameLayer`](Diagnostics/Example.Arch005.SameLayer) | [`ARCH005 - Same-layer dependency`](../README.md#arch005---same-layer-dependency) |
| [`Example.Arch006.UnknownLayer`](Diagnostics/Example.Arch006.UnknownLayer) | [`ARCH006 - Invalid architecture configuration`](../README.md#arch006---invalid-architecture-configuration) |
| [`Example.Arch007.CyclicGraph`](Diagnostics/Example.Arch007.CyclicGraph) | [`ARCH007 - Cyclic architecture dependency graph`](../README.md#arch007---cyclic-architecture-dependency-graph) |

### Features

| Folder | Main README section |
| ------ | ------------------- |
| [`Example.AllowedSites`](Features/Example.AllowedSites) | [`Site filters`](../README.md#site-filters) |
| [`Example.AllowedTypes`](Features/Example.AllowedTypes) | [`<Allowed>` type policies](../README.md#allowed-type-policy) |
| [`Example.ArchitectureHealth`](Features/Example.ArchitectureHealth) | [`Architecture health`](../README.md#architecture-health) |
| [`Example.AssemblyMatcher`](Features/Example.AssemblyMatcher) | [`Matcher types`](../README.md#matcher-types) |
| [`Example.BlockedDependency`](Features/Example.BlockedDependency) | [`<BlockedDependency>`](../README.md#blockeddependency) |
| [`Example.CascadingDependencyRules`](Features/Example.CascadingDependencyRules) | [`<AllowedDependency>`](../README.md#alloweddependency) |
| [`Example.CombinedMatchers`](Features/Example.CombinedMatchers) | [`Matcher types`](../README.md#matcher-types) |
| [`Example.WildcardTo`](Features/Example.WildcardTo) | [`<AllowedDependency>`](../README.md#alloweddependency) |
| [`Example.Exceptions`](Features/Example.Exceptions) | [`<Exceptions>`](../README.md#exceptions) |
| [`Example.IncludeSettings`](Features/Example.IncludeSettings) | [`<Include>`](../README.md#include) |
| [`Example.InlineXml`](Features/Example.InlineXml) | [`Optional: inline settings with AssemblyMetadata`](../README.md#5-optional-inline-settings-with-assemblymetadata) |
| [`Example.LayerScopedRecognizedDependencies`](Features/Example.LayerScopedRecognizedDependencies) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.NestedExceptions`](Features/Example.NestedExceptions) | [`Nesting`](../README.md#nesting) |
| [`Example.NestedLayers`](Features/Example.NestedLayers) | [`Hierarchical layer boundaries`](../README.md#hierarchical-layer-boundaries) |
| [`Example.NonClassCallers`](Features/Example.NonClassCallers) | [`Diagnostic properties`](../README.md#diagnostic-properties) |
| [`Example.RequiredRecognizedDependencySites`](Features/Example.RequiredRecognizedDependencySites) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.SameLayerInheritance`](Features/Example.SameLayerInheritance) | [`ARCH005 - Same-layer dependency`](../README.md#arch005---same-layer-dependency) |
| [`Example.ScopedTypePolicies`](Features/Example.ScopedTypePolicies) | [`Layer-scoped type policies`](../README.md#layer-scoped-type-policies) |

### Scenarios

| Folder | Scenario |
| ------ | -------- |
| [`Example.RepositoryQuerySurface`](Scenarios/Example.RepositoryQuerySurface) | Repository-owned fluent query surface that must be projected before it becomes an application dependency. |

### Documentation

| Folder | Main README section |
| ------ | ------------------- |
| [`Example.DocumentationDemo`](Documentation/Example.DocumentationDemo) | [`Architecture documentation`](../README.md#architecture-documentation), [`description` attributes](../README.md#description-attributes) |
| [`Example.ReportDemo`](Documentation/Example.ReportDemo) | [`Violation report`](../README.md#violation-report) |

## Running an example

```cmd
dotnet build Examples\Diagnostics\Example.Arch003.ForbiddenType -c Release
```

Expected output — one `error ARCH003` and a failed build. That's the example doing its job. Simple one-file examples keep their top-level rule set in `AssemblyMetadata("AnaalIJzerSettings", ...)`; broader examples use `Architecture.anl`.

To regenerate the committed example report and documentation:

```cmd
dotnet run --project src\Tools\RonSijm.AnaalIJzer.Arse -- report --project Examples\Documentation\Example.ReportDemo\Example.ReportDemo.csproj --force
Examples\Documentation\Example.DocumentationDemo\GenerateDocumentation.bat
```

The files in [`Documentation/Generated/`](Documentation/Generated) are rewritten by those commands. The analyzer itself only reports diagnostics.
