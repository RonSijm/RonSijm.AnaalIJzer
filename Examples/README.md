# Examples

This folder contains runnable sample projects for **RonSijm.AnaalIJzer**. Simple one-file examples keep their top-level config inline in the example source file with `AssemblyMetadata("AnaalIJzerSettings", ...)`, so exact type rules can use `nameof(...)` and stay refactor-safe. Broader multi-file, scenario, or include-focused examples use `Architecture.anl`, which keeps larger rule sets easier to scan. Most examples intentionally fail with documented architectural diagnostics, while a few demonstrate clean configuration or generated documentation output.

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

When `true`, the props file adds the analyzer `ProjectReference` (with `OutputItemType="Analyzer"`) and registers project-local settings files, including project-local `.anl` subfolders, as `AdditionalFiles` so inline configs can still use `<Include>`.

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
| [`Example.Arch009.ApiSurfaceLeakage`](Diagnostics/Example.Arch009.ApiSurfaceLeakage) | [`API surface policies`](../README.md#api-surface-policies) |
| [`Example.Arch012.VisibilityPolicy`](Diagnostics/Example.Arch012.VisibilityPolicy) | [`Visibility policies`](../README.md#visibility-policies) |
| [`Example.Arch013.ContractPurity`](Diagnostics/Example.Arch013.ContractPurity) | [`Contract purity`](../README.md#contract-purity) |
| [`Example.Arch013.ContractPurity.MethodBodyNotAllowed`](Diagnostics/Example.Arch013.ContractPurity.MethodBodyNotAllowed) | [`Contract purity`](../README.md#contract-purity) |
| [`Example.Arch014.TransitiveExposure`](Diagnostics/Example.Arch014.TransitiveExposure) | [`Transitive API exposure`](../README.md#transitive-api-exposure) |
| [`Example.Arch016.BoundaryEntryPoints`](Diagnostics/Example.Arch016.BoundaryEntryPoints) | [`ARCH016 - Boundary entry-point violation`](../README.md#arch016---boundary-entry-point-violation) |
| [`Example.Arch018.ObservedCycle`](Diagnostics/Example.Arch018.ObservedCycle) | [`ARCH018 - Observed architectural dependency cycle`](../README.md#arch018---observed-architectural-dependency-cycle) |
| [`Example.Arch019.InheritancePolicy`](Diagnostics/Example.Arch019.InheritancePolicy) | `InheritancePolicy` - layer-scoped required base types and interfaces. |
| [`Example.Arch020.ExplicitNullReturn`](Diagnostics/Example.Arch020.ExplicitNullReturn) | `ReturnValuePolicy` - a configured `Literal value="null"` is not an acceptable serving decision. |
| [`Example.Arch020.AnnotatedInvocationReturn`](Diagnostics/Example.Arch020.AnnotatedInvocationReturn) | `ReturnValuePolicy` - a configured nullable-result annotation must be handled before an invocation is returned. |
| [`Example.Arch020.ConfiguredLiteralReturns`](Diagnostics/Example.Arch020.ConfiguredLiteralReturns) | `ReturnValuePolicy` - empty-string, numeric, and enum-zero sentinel values are configurable literal matchers. |
| [`Example.Arch021.ClockAccess`](Diagnostics/Example.Arch021.ClockAccess) | `ForbiddenOperations` - `DateTime.UtcNow`, `DateTime.Now`, and `DateTime.Today` are selected forbidden clock reads. |
| [`Example.Arch021.BlockingTaskAccess`](Diagnostics/Example.Arch021.BlockingTaskAccess) | `ForbiddenOperations` - `Task.Wait()` and `Task<T>.Result` are selected blocking task operations at their own sites. |
| [`Example.Arch021.ServiceLocation`](Diagnostics/Example.Arch021.ServiceLocation) | `ForbiddenOperations` - `IServiceProvider.GetService` is forbidden outside the composition root. |
| [`Example.Arch021.SelectedEnvironmentMember`](Diagnostics/Example.Arch021.SelectedEnvironmentMember) | `ForbiddenOperations` - one `Environment` property is forbidden while unrelated properties remain allowed. |
| [`Example.Arch022.RequiredOperation`](Diagnostics/Example.Arch022.RequiredOperation) | `BehavioralOperations` - a selected kitchen method must perform one required operation. |
| [`Example.Arch022.RequiredOperationBefore`](Diagnostics/Example.Arch022.RequiredOperationBefore) | `BehavioralOperations` - a required safety check must dominate the selected oven mutation. |
| [`Example.Arch022.ForbiddenOperationAfter`](Diagnostics/Example.Arch022.ForbiddenOperationAfter) | `BehavioralOperations` - a selected operation is forbidden after a configured terminal operation. |
| [`Example.Arch022.MaximumOperationCount`](Diagnostics/Example.Arch022.MaximumOperationCount) | `BehavioralOperations` - a selected operation may occur only the configured number of times. |
| [`Example.Arch023.OperationContract`](Diagnostics/Example.Arch023.OperationContract) | `Operations` - an explicit waiter-to-kitchen operation contract checks entry-point delegation and request/response shape. |

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
| [`Example.DeclarationNameMatchesType`](Features/Example.DeclarationNameMatchesType) | [`NameRules`](../README.md#namerules) |
| [`Example.GeneratedCode`](Features/Example.GeneratedCode) | [`Generated code analysis`](../README.md#generated-code-analysis) - opt in only the generated files your team intentionally owns. |
| [`Example.DeclarationObservationMatchers`](Features/Example.DeclarationObservationMatchers) | Nested declaration matchers with code observations such as `<Throw />` and required companion interfaces. |
| [`Example.StructuralDeclarationMatchers`](Features/Example.StructuralDeclarationMatchers) | [`Matcher types`](../README.md#matcher-types) and [`InheritancePolicy`](../README.md#inheritance-policies) working together to describe a recognizable request shape. |
| [`Example.ExceptionPolicy`](Features/Example.ExceptionPolicy) | `ExceptionPolicy` and temporary architecture exception review warnings (`ARCH017`). |
| [`Example.WildcardTo`](Features/Example.WildcardTo) | [`<AllowedDependency>`](../README.md#alloweddependency) |
| [`Example.Exceptions`](Features/Example.Exceptions) | [`<Exceptions>`](../README.md#exceptions) |
| [`Example.IncludeSettings`](Features/Example.IncludeSettings) | [`<Include>`](../README.md#include) |
| [`Example.IncludeWildcardSettings`](Features/Example.IncludeWildcardSettings) | [`<Include>`](../README.md#include) |
| [`Example.InlineXml`](Features/Example.InlineXml) | [`Optional: inline settings with AssemblyMetadata`](../README.md#5-optional-inline-settings-with-assemblymetadata) |
| [`Example.LayerScopedRecognizedDependencies`](Features/Example.LayerScopedRecognizedDependencies) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.NameRuleIntraProceduralTracking`](Features/Example.NameRuleIntraProceduralTracking) | [`NameRules`](../README.md#namerules) with `valueTracking="Direct|IntraProcedural"` across local aliases and a lambda body. |
| [`Example.NameRuleLanguageForms`](Features/Example.NameRuleLanguageForms) | [`NameRules`](../README.md#namerules) across compound assignment, deconstruction, wrappers, expression bodies, and named arguments. |
| [`Example.NameRules`](Features/Example.NameRules) | [`NameRules`](../README.md#namerules) |
| [`Example.NestedExceptions`](Features/Example.NestedExceptions) | [`Nesting`](../README.md#nesting) |
| [`Example.NestedLayers`](Features/Example.NestedLayers) | [`Hierarchical layer boundaries`](../README.md#hierarchical-layer-boundaries) |
| [`Example.NonClassCallers`](Features/Example.NonClassCallers) | [`Diagnostic properties`](../README.md#diagnostic-properties) |
| [`Example.RequiredRecognizedDependencySites`](Features/Example.RequiredRecognizedDependencySites) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.SameLayerInheritance`](Features/Example.SameLayerInheritance) | [`ARCH005 - Same-layer dependency`](../README.md#arch005---same-layer-dependency) |
| [`Example.ScopedTypePolicies`](Features/Example.ScopedTypePolicies) | [`Layer-scoped type policies`](../README.md#layer-scoped-type-policies) |
| [`Example.SourceLocations`](Features/Example.SourceLocations) | [`Source locations`](../README.md#source-locations) |

### Scenarios

| Folder | Scenario |
| ------ | -------- |
| [`Example.AspNetCore`](Scenarios/Example.AspNetCore) | Real `Microsoft.NET.Sdk.Web` projects showing framework-neutral controller boundaries, action-name rules, explicit action contracts, and public query-surface protection. |
| [`Example.EntityFrameworkCore`](Scenarios/Example.EntityFrameworkCore) | Real Entity Framework Core projects showing DbContext ownership, creation-site restrictions, query-surface containment, folder ownership, and an optional persistence-ignorant domain policy. |
| [`Example.AssemblyReferenceBoundaries`](Scenarios/Example.AssemblyReferenceBoundaries) | Project-level scenario showing that a raw MSBuild `<Reference>` is surfaced by workspace inspection, without introducing a compiler `ARCHxxx` diagnostic. Project: `Example.AssemblyReferenceBoundaries.Domain`. |
| [`Example.HonestTypeEndpointNames`](Scenarios/Example.HonestTypeEndpointNames) | Strong endpoint parameter types whose convention-based binding names must still match their semantic types. |
| [`Example.PackageReferenceBoundaries`](Scenarios/Example.PackageReferenceBoundaries) | Multi-project scenario showing that a forbidden direct NuGet package reference raises `ARCH011` even when no source file uses a type from that package yet. Projects: `Example.PackageReferenceBoundaries.Domain`, `Example.PackageReferenceBoundaries.Data`. |
| [`Example.ProjectReferenceBoundaries`](Scenarios/Example.ProjectReferenceBoundaries) | Multi-project scenario showing that an illegal `.csproj` reference raises `ARCH010` even when no source file uses it yet. Projects: `Example.ProjectReferenceBoundaries.Application`, `Example.ProjectReferenceBoundaries.Domain`, `Example.ProjectReferenceBoundaries.Infrastructure`. |
| [`Example.ProjectReferenceRuleSelectors`](Scenarios/Example.ProjectReferenceRuleSelectors) | Multi-project scenario showing that `From` and `To` selectors narrow an `Application -> Contracts` project-group edge to one exact project pair. Projects: `Example.ProjectReferenceRuleSelectors.Orders.Application`, `Example.ProjectReferenceRuleSelectors.Orders.Contracts`, and `Example.ProjectReferenceRuleSelectors.Payments.Contracts`. |
| [`Example.RepositoryQuerySurface`](Scenarios/Example.RepositoryQuerySurface) | Repository-owned fluent query surface that must be projected before it becomes an application dependency. |
| [`Example.SolutionTopology`](Scenarios/Example.SolutionTopology) | Multi-project scenario showing a solution-wide logical module rule that Arse reports as `TOPO001` only when solution-topology enforcement is requested. |

### Documentation

| Folder | Main README section |
| ------ | ------------------- |
| [`Example.DocumentationDemo`](Documentation/Example.DocumentationDemo) | [`Architecture documentation`](../README.md#architecture-documentation), [`description` attributes](../README.md#description-attributes) |
| [`Example.ReportDemo`](Documentation/Example.ReportDemo) | [`Violation report`](../README.md#violation-report) |
| [`Example.VisualStudioSiteDiagnostics`](Documentation/Example.VisualStudioSiteDiagnostics) | Clean, one-file demonstration of every Visual Studio Layer Information and Site Diagnostics site. |

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
