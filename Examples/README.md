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

Diagnostic examples are grouped by the concern segment in their diagnostic ID. The [`Diagnostics/` index](Diagnostics) explains the categories.

| Concern | Folder | Main README section |
| ------- | ------ | ------------------- |
| [`API`](Diagnostics/API) | [`Example.Arch_API_001.ApiSurfaceLeakage`](Diagnostics/API/Example.Arch_API_001.ApiSurfaceLeakage) | [`API surface policies`](../README.md#api-surface-policies) |
| [`API`](Diagnostics/API) | [`Example.Arch_API_010.TransitiveExposure`](Diagnostics/API/Example.Arch_API_010.TransitiveExposure) | [`Transitive API exposure`](../README.md#transitive-api-exposure) |
| [`ASSM`](Diagnostics/ASSM) | [`Example.Arch_ASSM_001.AssemblyAttributePolicy.Code`](Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Code) | `AssemblyAttributePolicy` - a handwritten `InternalsVisibleTo` attribute is selectively rejected by its compiled semantic type and argument. |
| [`ASSM`](Diagnostics/ASSM) | [`Example.Arch_ASSM_001.AssemblyAttributePolicy.Project`](Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Project) | `AssemblyAttributePolicy` - an SDK `<InternalsVisibleTo>` project item produces the same selectively rejected compiled attribute. |
| [`BOUND`](Diagnostics/BOUND) | [`Example.Arch_BOUND_007.BoundaryEntryPoints`](Diagnostics/BOUND/Example.Arch_BOUND_007.BoundaryEntryPoints) | [`ARCH_BOUND_007 - Boundary entry-point violation`](../README.md#arch_bound_007---boundary-entry-point-violation) |
| [`CONF`](Diagnostics/CONF) | [`Example.Arch_CONF_003.UnknownLayer`](Diagnostics/CONF/Example.Arch_CONF_003.UnknownLayer) | [`ARCH_CONF_003 - Invalid architecture configuration`](../README.md#arch_conf_003---invalid-architecture-configuration) |
| [`CONF`](Diagnostics/CONF) | [`Example.Arch_CONF_006.CyclicGraph`](Diagnostics/CONF/Example.Arch_CONF_006.CyclicGraph) | [`ARCH_CONF_006 - Cyclic architecture dependency graph`](../README.md#arch_conf_006---cyclic-architecture-dependency-graph) |
| [`CONT`](Diagnostics/CONT) | [`Example.Arch_CONT_008.ContractPurity.MethodBodyNotAllowed`](Diagnostics/CONT/Example.Arch_CONT_008.ContractPurity.MethodBodyNotAllowed) | [`Contract purity`](../README.md#contract-purity) |
| [`CONT`](Diagnostics/CONT) | [`Example.Arch_CONT_008.ContractPurity`](Diagnostics/CONT/Example.Arch_CONT_008.ContractPurity) | [`Contract purity`](../README.md#contract-purity) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_001.GenericTypeArgument`](Diagnostics/DEP/Example.Arch_DEP_001.GenericTypeArgument) | [`Matcher types`](../README.md#matcher-types) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_001.NoEdge`](Diagnostics/DEP/Example.Arch_DEP_001.NoEdge) | [`ARCH_DEP_001 - Illegal layer dependency`](../README.md#arch_dep_001---illegal-layer-dependency) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_001.NonConstructorInjection`](Diagnostics/DEP/Example.Arch_DEP_001.NonConstructorInjection) | [`Diagnostic properties`](../README.md#diagnostic-properties) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_001.SkipsLayer`](Diagnostics/DEP/Example.Arch_DEP_001.SkipsLayer) | [`ARCH_DEP_001 - Illegal layer dependency`](../README.md#arch_dep_001---illegal-layer-dependency) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_002.UnrecognizedDependency`](Diagnostics/DEP/Example.Arch_DEP_002.UnrecognizedDependency) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_004.WrongDirection`](Diagnostics/DEP/Example.Arch_DEP_004.WrongDirection) | [`ARCH_DEP_004 - Wrong-direction dependency`](../README.md#arch_dep_004---wrong-direction-dependency) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_005.SameLayer`](Diagnostics/DEP/Example.Arch_DEP_005.SameLayer) | [`ARCH_DEP_005 - Same-layer dependency`](../README.md#arch_dep_005---same-layer-dependency) |
| [`DEP`](Diagnostics/DEP) | [`Example.Arch_DEP_006.ObservedCycle`](Diagnostics/DEP/Example.Arch_DEP_006.ObservedCycle) | [`ARCH_DEP_006 - Observed architectural dependency cycle`](../README.md#arch_dep_006---observed-architectural-dependency-cycle) |
| [`INH`](Diagnostics/INH) | [`Example.Arch_INH_001.InheritancePolicy`](Diagnostics/INH/Example.Arch_INH_001.InheritancePolicy) | `InheritancePolicy` - layer-scoped required base types and interfaces. |
| [`NS`](Diagnostics/NS) | [`Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant`](Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.AncestorToDescendant) | [`ARCH_NS_007 - Namespace hierarchy dependency violation`](../README.md#arch_ns_007---namespace-hierarchy-dependency-violation) - a root-level type reaches down into a feature namespace. |
| [`NS`](Diagnostics/NS) | [`Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor`](Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.DescendantToAncestor) | [`ARCH_NS_007 - Namespace hierarchy dependency violation`](../README.md#arch_ns_007---namespace-hierarchy-dependency-violation) - a feature namespace reaches back into its restaurant root. |
| [`NS`](Diagnostics/NS) | [`Example.Arch_NS_007.NamespaceHierarchy.SameNamespace`](Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SameNamespace) | [`ARCH_NS_007 - Namespace hierarchy dependency violation`](../README.md#arch_ns_007---namespace-hierarchy-dependency-violation) - types within one strict feature namespace reference each other. |
| [`NS`](Diagnostics/NS) | [`Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling`](Diagnostics/NS/Example.Arch_NS_007.NamespaceHierarchy.SiblingToSibling) | [`ARCH_NS_007 - Namespace hierarchy dependency violation`](../README.md#arch_ns_007---namespace-hierarchy-dependency-violation) - two sibling restaurant features take a direct dependency. |
| [`OPCT`](Diagnostics/OPCT) | [`Example.Arch_OPCT_001.ParticipantNotAllowed`](Diagnostics/OPCT/Example.Arch_OPCT_001.ParticipantNotAllowed) | `ARCH_OPCT_001` - a selected operation owner belongs to a layer that the contract does not allow. |
| [`OPCT`](Diagnostics/OPCT) | [`Example.Arch_OPCT_002.RequiredOwnerInvocation`](Diagnostics/OPCT/Example.Arch_OPCT_002.RequiredOwnerInvocation) | `ARCH_OPCT_002` - a selected entry point does not invoke the configured owner. |
| [`OPCT`](Diagnostics/OPCT) | [`Example.Arch_OPCT_008.ResponseShapeMismatch`](Diagnostics/OPCT/Example.Arch_OPCT_008.ResponseShapeMismatch) | `ARCH_OPCT_008` - a selected owner returns a type that does not match the configured response shape. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_001.BlockingTaskAccess`](Diagnostics/OPER/Example.Arch_OPER_001.BlockingTaskAccess) | `ForbiddenOperations` - `Task.Wait()` and `Task<T>.Result` are selected blocking task operations at their own sites. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_001.ClockAccess`](Diagnostics/OPER/Example.Arch_OPER_001.ClockAccess) | `ForbiddenOperations` - `DateTime.UtcNow`, `DateTime.Now`, and `DateTime.Today` are selected forbidden clock reads. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_001.SelectedEnvironmentMember`](Diagnostics/OPER/Example.Arch_OPER_001.SelectedEnvironmentMember) | `ForbiddenOperations` - one `Environment` property is forbidden while unrelated properties remain allowed. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_001.ServiceLocation`](Diagnostics/OPER/Example.Arch_OPER_001.ServiceLocation) | `ForbiddenOperations` - `IServiceProvider.GetService` is forbidden outside the composition root. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_002.RequiredOperation`](Diagnostics/OPER/Example.Arch_OPER_002.RequiredOperation) | `BehavioralOperations` - a selected kitchen method must perform one required operation. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_011.MaximumOperationCount`](Diagnostics/OPER/Example.Arch_OPER_011.MaximumOperationCount) | `BehavioralOperations` - a selected operation may occur only the configured number of times. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_012.ForbiddenOperationAfter`](Diagnostics/OPER/Example.Arch_OPER_012.ForbiddenOperationAfter) | `BehavioralOperations` - a selected operation is forbidden after a configured terminal operation. |
| [`OPER`](Diagnostics/OPER) | [`Example.Arch_OPER_012.RequiredOperationBefore`](Diagnostics/OPER/Example.Arch_OPER_012.RequiredOperationBefore) | `BehavioralOperations` - a required safety check must dominate the selected oven mutation. |
| [`RET`](Diagnostics/RET) | [`Example.Arch_RET_001.AnnotatedInvocationReturn`](Diagnostics/RET/Example.Arch_RET_001.AnnotatedInvocationReturn) | `ReturnValuePolicy` - a configured nullable-result annotation must be handled before an invocation is returned. |
| [`RET`](Diagnostics/RET) | [`Example.Arch_RET_001.ConfiguredLiteralReturns`](Diagnostics/RET/Example.Arch_RET_001.ConfiguredLiteralReturns) | `ReturnValuePolicy` - empty-string, numeric, and enum-zero sentinel values are configurable literal matchers. |
| [`RET`](Diagnostics/RET) | [`Example.Arch_RET_001.ExplicitNullReturn`](Diagnostics/RET/Example.Arch_RET_001.ExplicitNullReturn) | `ReturnValuePolicy` - a configured `Literal value="null"` is not an acceptable serving decision. |
| [`RET`](Diagnostics/RET) | [`Example.Arch_RET_001.DirectInvocationReturn`](Diagnostics/RET/Example.Arch_RET_001.DirectInvocationReturn) | `ReturnValuePolicy` - `<Invocation />` rejects a directly returned method call without blocking literal or null returns. |
| [`TYPE`](Diagnostics/TYPE) | [`Example.Arch_TYPE_001.ForbiddenType`](Diagnostics/TYPE/Example.Arch_TYPE_001.ForbiddenType) | [`<Forbidden>`](../README.md#forbidden) |
| [`VIS`](Diagnostics/VIS) | [`Example.Arch_VIS_001.VisibilityPolicy`](Diagnostics/VIS/Example.Arch_VIS_001.VisibilityPolicy) | [`Visibility policies`](../README.md#visibility-policies) |

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
| [`Example.ExceptionPolicy`](Features/Example.ExceptionPolicy) | `ExceptionPolicy` and temporary architecture exception review warnings (`ARCH_EXC_009`). |
| [`Example.WildcardTo`](Features/Example.WildcardTo) | [`<AllowedDependency>`](../README.md#alloweddependency) |
| [`Example.Exceptions`](Features/Example.Exceptions) | [`<Exceptions>`](../README.md#exceptions) |
| [`Example.IncludeSettings`](Features/Example.IncludeSettings) | [`<Include>`](../README.md#include) |
| [`Example.IncludeWildcardSettings`](Features/Example.IncludeWildcardSettings) | Wildcard [`<Include>`](../README.md#include) rules, including an optional drop-in folder that may be empty. |
| [`Example.GlobalReturnValuePolicy`](Features/Example.GlobalReturnValuePolicy) | A root `<ReturnValuePolicy>` imported from a wildcard rule folder and applied without layers. |
| [`Example.InlineXml`](Features/Example.InlineXml) | [`Optional: inline settings with AssemblyMetadata`](../README.md#5-optional-inline-settings-with-assemblymetadata) |
| [`Example.LayerScopedRecognizedDependencies`](Features/Example.LayerScopedRecognizedDependencies) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.NameRuleIntraProceduralTracking`](Features/Example.NameRuleIntraProceduralTracking) | [`NameRules`](../README.md#namerules) with `valueTracking="Direct|IntraProcedural"` across local aliases and a lambda body. |
| [`Example.NameRuleLanguageForms`](Features/Example.NameRuleLanguageForms) | [`NameRules`](../README.md#namerules) across compound assignment, deconstruction, wrappers, expression bodies, and named arguments. |
| [`Example.NameRules`](Features/Example.NameRules) | [`NameRules`](../README.md#namerules) |
| [`Example.NestedExceptions`](Features/Example.NestedExceptions) | [`Nesting`](../README.md#nesting) |
| [`Example.NestedLayers`](Features/Example.NestedLayers) | [`Hierarchical layer boundaries`](../README.md#hierarchical-layer-boundaries) |
| [`Example.NamespaceHierarchySites`](Features/Example.NamespaceHierarchySites) | [`Namespace hierarchy policies`](../README.md#namespace-hierarchy-policies) - every dependency site under both `allowedSites` and `blockedSites` filters. |
| [`Example.NonClassCallers`](Features/Example.NonClassCallers) | [`Diagnostic properties`](../README.md#diagnostic-properties) |
| [`Example.RequiredRecognizedDependencySites`](Features/Example.RequiredRecognizedDependencySites) | [`requireRecognizedDependencies`](../README.md#requirerecognizeddependencies-attribute) |
| [`Example.SameLayerInheritance`](Features/Example.SameLayerInheritance) | [`ARCH_DEP_005 - Same-layer dependency`](../README.md#arch_dep_005---same-layer-dependency) |
| [`Example.ScopedTypePolicies`](Features/Example.ScopedTypePolicies) | [`Layer-scoped type policies`](../README.md#layer-scoped-type-policies) |
| [`Example.SourceLocations`](Features/Example.SourceLocations) | [`Source locations`](../README.md#source-locations) |

### Scenarios

| Folder | Scenario |
| ------ | -------- |
| [`Example.AspNetCore`](Scenarios/Example.AspNetCore) | Real `Microsoft.NET.Sdk.Web` projects showing framework-neutral controller boundaries, action-name rules, explicit action contracts, and public query-surface protection. |
| [`Example.EntityFrameworkCore`](Scenarios/Example.EntityFrameworkCore) | Real Entity Framework Core projects showing DbContext ownership, creation-site restrictions, query-surface containment, folder ownership, and an optional persistence-ignorant domain policy. |
| [`Example.AssemblyReferenceBoundaries`](Scenarios/Example.AssemblyReferenceBoundaries) | Project-level scenario showing that a raw MSBuild `<Reference>` is surfaced by workspace inspection, without introducing a compiler `ARCHxxx` diagnostic. Project: `Example.AssemblyReferenceBoundaries.Domain`. |
| [`Example.HonestTypeEndpointNames`](Scenarios/Example.HonestTypeEndpointNames) | Strong endpoint parameter types whose convention-based binding names must still match their semantic types. |
| [`Example.PackageReferenceBoundaries`](Scenarios/Example.PackageReferenceBoundaries) | Multi-project scenario showing that a forbidden direct NuGet package reference raises `ARCH_PKG_001` even when no source file uses a type from that package yet. Projects: `Example.PackageReferenceBoundaries.Domain`, `Example.PackageReferenceBoundaries.Data`. |
| [`Example.ProjectReferenceBoundaries`](Scenarios/Example.ProjectReferenceBoundaries) | Multi-project scenario showing that an illegal `.csproj` reference raises `ARCH_PROJ_001` even when no source file uses it yet. Projects: `Example.ProjectReferenceBoundaries.Application`, `Example.ProjectReferenceBoundaries.Domain`, `Example.ProjectReferenceBoundaries.Infrastructure`. |
| [`Example.ProjectReferenceRuleSelectors`](Scenarios/Example.ProjectReferenceRuleSelectors) | Multi-project scenario showing that `From` and `To` selectors narrow an `Application -> Contracts` project-group edge to one exact project pair. Projects: `Example.ProjectReferenceRuleSelectors.Orders.Application`, `Example.ProjectReferenceRuleSelectors.Orders.Contracts`, and `Example.ProjectReferenceRuleSelectors.Payments.Contracts`. |
| [`Example.RepositoryQuerySurface`](Scenarios/Example.RepositoryQuerySurface) | Repository-owned fluent query surface that must be projected before it becomes an application dependency. |
| [`Example.SolutionTopology`](Scenarios/Example.SolutionTopology) | Multi-project scenario showing a solution-wide logical module rule that Arse reports as `ARCH_SOL_001` only when solution-topology enforcement is requested. |

### Documentation

| Folder | Main README section |
| ------ | ------------------- |
| [`Example.DocumentationDemo`](Documentation/Example.DocumentationDemo) | [`Architecture documentation`](../README.md#architecture-documentation), [`description` attributes](../README.md#description-attributes) |
| [`Example.ReportDemo`](Documentation/Example.ReportDemo) | [`Violation report`](../README.md#violation-report) |
| [`Example.VisualStudioSiteDiagnostics`](Documentation/Example.VisualStudioSiteDiagnostics) | Clean, one-file demonstration of every Visual Studio Layer Information and Site Diagnostics site. |

## Running an example

```cmd
dotnet build Examples\Diagnostics\TYPE\Example.Arch_TYPE_001.ForbiddenType -c Release
```

Expected output — one `error ARCH_TYPE_001` and a failed build. That's the example doing its job. Simple one-file examples keep their top-level rule set in `AssemblyMetadata("AnaalIJzerSettings", ...)`; broader examples use `Architecture.anl`.

To regenerate the committed example report and documentation:

```cmd
dotnet run --project src\Tools\RonSijm.AnaalIJzer.Arse -- report --project Examples\Documentation\Example.ReportDemo\Example.ReportDemo.csproj --force
Examples\Documentation\Example.DocumentationDemo\GenerateDocumentation.bat
```

The files in [`Documentation/Generated/`](Documentation/Generated) are rewritten by those commands. The analyzer itself only reports diagnostics.
