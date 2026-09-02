# Example Catalog Review

Last reviewed: 2026-09-02

## Purpose

This is a planning and review aid for the runnable projects under `Examples/`. It is not public product documentation and it does not change analyzer behavior.

The catalog has two jobs:

1. record the single lesson each project is meant to teach;
2. decide whether an example benefits from a human-friendly domain story or should remain deliberately technical.

There are currently **56 runnable example projects**. Each is covered below, including every child project in the multi-project scenarios.

## Vocabulary Rule

Do not force every example into a restaurant metaphor. The restaurant is useful because it explains dependency direction without assuming that a reader already knows MVC, CQRS, vertical slices, or a particular framework. It is not a substitute for precise language about compiler behavior, project references, XML configuration, or type-system rules.

Use one vocabulary per example:

- **Restaurant boundary vocabulary:** `Customer -> Waiter -> Chef -> Pantry`. Use it for dependencies, forbidden directions, API exposure, entry points, and site filters. An arrow always means "may depend on", never runtime request or data flow.
- **Pizza request vocabulary:** `PizzaRequest`, `PizzaId`, `PizzaProvider`, and related types. Use it for structural declaration matching and code-observation examples. This is a self-contained domain, not a mapping to the restaurant layers.
- **Identity vocabulary:** `PatientId`, `DoctorId`, `CustomerId`, and similar semantic types. Use it for naming and binding rules, where a restaurant story would hide the actual risk.
- **Technical vocabulary:** `Application`, `Domain`, `Infrastructure`, `Assembly`, `ProjectReference`, configuration, or Roslyn sites. Use it for language, tooling, and configuration mechanics.
- **Scenario-local vocabulary:** preserve a coherent specialized domain, such as the `OrderRepository -> OrderQuery -> OrderProjection` query-surface scenario. Do not mix it with Waiters and Chefs.

The recommended status in each row means:

| Status | Meaning |
|---|---|
| Keep | The current lesson already has a coherent vocabulary. Keep its names aligned when editing it. |
| Clarify | The behavior is useful, but comments, type names, or the README should be normalized to the suggested vocabulary in a future focused change. |
| Technical | Keep the example precise and technical. A metaphor would make the lesson less understandable. |
| Shared scenario | This project is one role in a multi-project scenario. It is not a standalone story. |

## Review Criteria

Every future example edit should pass these checks:

- It has one primary lesson and a reader can say what that lesson is after reading the source file name and one comment.
- The project name, configuration, source comments, expected diagnostic, local README, and linked main-document section agree.
- A restaurant example uses only restaurant roles. It never says that a `Controller` is a Waiter or that an `Application` is a Chef.
- A technical example does not add restaurant names merely to make it friendlier.
- A multi-project scenario names the role of each project and explains the scenario at its parent folder.
- Intentional diagnostics remain explicit in a nearby comment; passing projects state what makes them valid.

## Diagnostic Projects

| Project | Primary use case | Suggested teaching vocabulary | Status and review action |
|---|---|---|---|
| `Diagnostics/Example.Arch001.GenericTypeArgument` | Shows that a prohibited dependency remains prohibited inside `Lazy<T>`, `Func<T>`, collections, and other generic wrappers. It produces three `ARCH001` diagnostics. | Restaurant: a Customer asking for a Chef later, or asking for a group of Chefs, is still bypassing the Waiter. | Keep. The wrapper metaphor is concrete and maps cleanly to the rule. Ensure all comments use only Customer, Waiter, and Chef. |
| `Diagnostics/Example.Arch001.NoEdge` | Shows a direct dependency for which no allowed edge exists. It produces one `ARCH001`. | Restaurant: a Waiter may ask a Chef, but may not enter the Pantry directly. | Keep. This is the smallest useful missing-edge story. |
| `Diagnostics/Example.Arch001.NonConstructorInjection` | Shows that the same illegal relationship is detected at non-constructor sites, including members, locals, returns, object creation, generic use, attributes, and static use. It produces eight `ARCH001` diagnostics. | Restaurant: a Waiter cannot acquire a Pantry dependency by hiding it in a field, local note, return value, or helper shape. | Clarify. Keep one forbidden `Waiter -> Pantry` relationship throughout and make each source file name state the site it demonstrates. |
| `Diagnostics/Example.Arch001.SkipsLayer` | Shows that a caller cannot bypass an intermediary layer even though each adjacent edge is allowed. It produces one `ARCH001`. | Restaurant: a Customer orders through a Waiter; the Customer does not direct the Chef. | Keep. This is the canonical first dependency story. |
| `Diagnostics/Example.Arch002.UnrecognizedDependency` | Shows that a required dependency site must resolve to a configured layer. It produces one `ARCH002`. | Restaurant: an audited Chef must identify every ingredient it takes into the kitchen; an unknown `MysteryBox` is not acceptable. | Clarify. Keep the mystery-ingredient explanation, but reserve it for recognition rather than an illegal dependency edge. |
| `Diagnostics/Example.Arch003.ForbiddenType` | Shows a forbidden type policy independently of layer-direction rules. It produces one `ARCH003`. | Technical type-policy vocabulary. If a domain analogy is wanted, say a `Store` naming pattern is not allowed for infrastructure, but do not imply an actual dependency direction. | Technical. The important distinction is type policy versus allowed dependency edge. |
| `Diagnostics/Example.Arch004.WrongDirection` | Shows a reverse dependency when the opposite direction is allowed. It produces one `ARCH004`. | Restaurant: a Pantry does not manage a Chef; the Chef uses the Pantry. | Keep. This is a strong, memorable direction metaphor. |
| `Diagnostics/Example.Arch005.SameLayer` | Shows a dependency between peers in the same layer where no self-edge exists. It produces one `ARCH005`. | Restaurant: one Chef should not command another Chef merely because both work in the kitchen. | Keep. State that a deliberately configured self-edge is possible when the design needs it. |
| `Diagnostics/Example.Arch006.UnknownLayer` | Shows invalid configuration that references a layer that does not exist. It produces one `ARCH006`. | Technical configuration vocabulary. | Technical. The configuration error should remain literal and easy to copy into a repair. |
| `Diagnostics/Example.Arch007.CyclicGraph` | Shows a cycle in the configured allowed-dependency graph when acyclicity is enforced. It produces one `ARCH007`. | Technical graph vocabulary: configured edges form a cycle. | Technical. An everyday actor story would confuse configured permission with observed runtime behavior. |
| `Diagnostics/Example.Arch009.ApiSurfaceLeakage` | Shows a public API exposing a type that should remain internal to an architectural boundary. It produces one `ARCH009`. | Restaurant: the Customer can receive a plated dish, not a raw Pantry ingredient or a kitchen-only tool. | Keep. This conveys public-surface leakage without mixing technical layer names. |
| `Diagnostics/Example.Arch012.VisibilityPolicy` | Shows a visibility policy that rejects a declaration with the wrong accessibility. It produces one `ARCH012`. | Restaurant: a prep-area tool is not public dining-room equipment. | Clarify. State that this is a declaration-visibility rule, not a dependency rule. |
| `Diagnostics/Example.Arch013.ContractPurity` | Shows contract purity by rejecting a property setter where a contract allows getters only. It produces one `ARCH013`. | Restaurant: a menu describes what can be ordered; it does not accept mutable kitchen state. | Clarify. The source file should focus only on the setter violation and not show unrelated contract-policy switches. |
| `Diagnostics/Example.Arch013.ContractPurity.MethodBodyNotAllowed` | Shows contract purity by rejecting a method body where only signatures are permitted. It produces one `ARCH013`. | Restaurant: a menu or order contract declares an option; it does not perform the cooking. | Keep. This is distinct from the setter example and should remain in a separately named project. |
| `Diagnostics/Example.Arch014.TransitiveExposure` | Shows that a public type cannot hide a forbidden type inside a transitive generic or nested API shape. It produces one `ARCH014`. | Restaurant: a plated dish description must not secretly expose a raw Pantry ingredient inside its packaging. | Clarify. Explain the nested/transitive part explicitly; do not make it sound like ordinary runtime composition. |
| `Diagnostics/Example.Arch016.BoundaryEntryPoints` | Shows that outside callers can enter a boundary only through configured contracts. It produces one `ARCH016`. | Restaurant: a Customer enters ordering through the menu or Waiter, not directly through a kitchen workbench. | Keep. The parent-boundary versus internal-call distinction is the core lesson. |
| `Diagnostics/Example.Arch018.ObservedCycle` | Shows an actual source-code dependency cycle that is permitted by configuration but rejected by observed-cycle enforcement. It produces one `ARCH018`. | Technical graph vocabulary: configuration allows both directions, but the current source actually closes the loop. | Technical. Keep it separate from ARCH007 and avoid a restaurant analogy that blurs the two cycle concepts. |
| `Diagnostics/Example.Arch019.InheritancePolicy` | Shows a layer-scoped required base type or interface. It produces one `ARCH019`. | Technical contract vocabulary: selected declarations must implement the designated contract or inherit the designated base. | Technical. The rule is about declaration shape, not layer traffic. |
| `Diagnostics/Example.Arch020.AnnotatedInvocationReturn` | Shows a configurable annotation-based rule requiring a nullable invocation result to be handled before it is returned. It produces one `ARCH020`. | Restaurant: if the kitchen may have no pizza available, it must turn that absence into a defined serving decision before handing it on. | Keep. State explicitly that the annotation match is configuration-only and has no dependency on JetBrains.Annotations. |
| `Diagnostics/Example.Arch020.ConfiguredLiteralReturns` | Shows configurable forbidden literal returns such as an empty string, `42`, and enum zero. It produces three `ARCH020` diagnostics. | Technical return-policy vocabulary, optionally "do not use a magic fallback serving." | Technical. The point is configurability, so do not pretend all sentinel values share one natural restaurant meaning. |
| `Diagnostics/Example.Arch020.ExplicitNullReturn` | Shows that a configured literal `null` cannot be returned directly. It produces one `ARCH020`. | Restaurant: a Chef must return a defined serving result, not an empty hand. | Keep. Pair it with the configurable-literals example so readers see that `null` is one policy value, not a hard-coded analyzer opinion. |

## Feature Projects

| Project | Primary use case | Suggested teaching vocabulary | Status and review action |
|---|---|---|---|
| `Features/Example.AllowedSites` | Demonstrates each supported site with both allowlist and blocklist variants. It produces 26 `ARCH001` diagnostics. | Restaurant: the relationship is legal only at a named place of use, such as briefly returning a plated dish, but not storing a raw Pantry dependency. | Clarify. This is a site taxonomy, so every file should name the site first and use one stable `Waiter -> Chef` or `Waiter -> Pantry` story. |
| `Features/Example.AllowedTypes` | Demonstrates an `<Allowed>` type policy that rejects a type outside an allowlist. It produces one `ARCH003`. | Technical type-policy vocabulary: approved dependency shapes. | Technical. Emphasize that `<Allowed>` is unrelated to `<AllowedDependency>`. |
| `Features/Example.ArchitectureHealth` | Demonstrates a health inspection that identifies configuration drift, unused edges, stale exceptions, unmatched matchers, and unclassified types without creating an ordinary source diagnostic. | Technical tooling vocabulary: architecture health report. | Technical. Keep its output-focused README; it is intentionally not a single compile-error lesson. |
| `Features/Example.AssemblyMatcher` | Demonstrates classifying types by assembly and enforcing an edge based on those classifications. It produces one `ARCH004`. | Technical assembly vocabulary. | Technical. An assembly boundary needs no fictional actor mapping. |
| `Features/Example.BlockedDependency` | Demonstrates that an explicit blocked edge denies a relationship even if another rule would allow it. It produces one `ARCH001`. | Restaurant: the restaurant expressly forbids a Customer from entering the Pantry, even if a broad rule otherwise permits access. | Keep. Make the priority of the explicit block clear. |
| `Features/Example.CascadingDependencyRules` | Demonstrates `appliesToDescendants="true"`, allowing nested layers to use a top-level framework/crosscutting rule without repeating local egress rules. It builds cleanly. | Technical hierarchy vocabulary: a root framework rule intentionally cascades through descendant boundary gates. | Technical. The subtle boundary-gate behavior benefits from literal path names. |
| `Features/Example.CombinedMatchers` | Demonstrates AND semantics within one matcher element and OR semantics between sibling matcher elements, including `typeKind`. It produces one `ARCH005`. | Technical declaration-matcher vocabulary: interface repository versus class repository. | Technical. The contrast between contract and implementation is clearer than an actor metaphor. |
| `Features/Example.DeclarationNameMatchesType` | Demonstrates name rules that require a declaration name to agree with its semantic type. It produces six `ARCH008` diagnostics. | Identity vocabulary: `PatientId patientId`, `DoctorId doctorId`, and semantic identifier names. | Keep. Strong types and external conventions are the actual concern. |
| `Features/Example.DeclarationObservationMatchers` | Demonstrates nested declaration matchers that observe a method or property body, such as a `<Throw />`, then require a companion interface. It produces two `ARCH019` diagnostics. | Pizza request vocabulary: a pizza delivery operation that throws requires a fallback contract; a pizza catalog guard is analogous. | Keep. This is a coherent self-contained pizza domain. |
| `Features/Example.ExceptionPolicy` | Demonstrates policy-controlled temporary architecture exceptions and their review warning. It produces one `ARCH017`. | Restaurant: a temporary manager-approved kitchen pass must carry a review date or rationale. | Clarify. Keep the exception as a policy review, never as a hidden allowed dependency. |
| `Features/Example.Exceptions` | Demonstrates matcher `<Exceptions>` that remove a type from a matcher classification. It produces one `ARCH003`. | Technical matcher vocabulary. | Technical. Explain that it is a classification exclusion, not a dependency permission. |
| `Features/Example.IncludeSettings` | Demonstrates a larger XML-backed configuration split into included `.anl` files. It produces one `ARCH001`. | Technical configuration vocabulary: a root settings file composes rule files. | Technical. Preserve it as the readable, explicit include example. |
| `Features/Example.IncludeWildcardSettings` | Demonstrates `<Include path="*.anl" />` so a folder of drop-in rule packs can be enabled or removed without editing the root configuration. It produces one `ARCH001`. | Technical plugin/configuration vocabulary. | Technical. The drop-in rule-pack concept is clearer without a domain analogy. |
| `Features/Example.InlineXml` | Demonstrates inline settings through `AssemblyMetadata("AnaalIJzerSettings", ...)` and refactor-safe `nameof` expressions. It produces one `ARCH001`. | Technical source-configuration vocabulary. | Technical. Keep the one-file shape and make the metadata ownership obvious. |
| `Features/Example.LayerScopedRecognizedDependencies` | Demonstrates `requireRecognizedDependencies` applied only inside one layer. It produces one `ARCH002`. | Restaurant: an audited Chef must identify ingredients, while a legacy area is not yet subject to that requirement. | Clarify. Make the scoped-adoption story explicit and keep it distinct from forbidden edge examples. |
| `Features/Example.NameRules` | Demonstrates semantic-name policies for assignments, arguments, returns, and selected sites. It produces four `ARCH008` diagnostics. | Identity vocabulary: `fruitId` and `animalId`, or business IDs with meaningful names. | Keep. Do not turn this into a restaurant example; accidental value swapping is clearer with IDs. |
| `Features/Example.NestedExceptions` | Demonstrates recursively nested matcher exclusions, where each level excludes a prior exception. It produces two `ARCH001` diagnostics. | Technical matcher vocabulary. | Technical. The behavior is deliberately unusual and needs exact configuration terms. |
| `Features/Example.NestedLayers` | Demonstrates hierarchical layer boundaries, direct parent membership, repeated local child names, and root/egress/ingress gates. It includes several targeted `ARCH001` cases. | Technical bounded-context vocabulary: `Ordering`, `Billing`, `Application`, `Repository`, and `Contracts`. | Technical. Restaurant locations could explain a small subset but would obscure the multi-gate rule. |
| `Features/Example.NonClassCallers` | Demonstrates that records, structs, interfaces, or other supported declaration forms can be callers. It produces three `ARCH001` diagnostics. | Technical C# declaration vocabulary. | Technical. The lesson is analyzer coverage across language constructs. |
| `Features/Example.RequiredRecognizedDependencySites` | Demonstrates requiring recognized dependency targets at selected sites. It produces 13 `ARCH002` diagnostics. | Restaurant: an audited Chef must recognize ingredients at the listed code locations. | Clarify. Use the same mystery-ingredient vocabulary as the smaller ARCH002 example while naming sites precisely. |
| `Features/Example.SameLayerInheritance` | Demonstrates a narrowly allowed self-edge at `Inheritance` or `InterfaceImplementation`, while ordinary same-layer calls remain prohibited. It produces one `ARCH005`. | Technical contract/implementation vocabulary: a repository implements its repository interface. | Keep. This directly answers a common C# design question and should not become a restaurant analogy. |
| `Features/Example.ScopedTypePolicies` | Demonstrates `<Allowed>` and `<Forbidden>` policies scoped to a layer and inherited by descendants. It produces two `ARCH003` diagnostics. | Technical policy-scope vocabulary, with optional Kitchen/Pantry names only when they describe a real local policy. | Technical. Scope inheritance is the lesson. |
| `Features/Example.SourceLocations` | Demonstrates that a type can have the correct namespace/layer but still live in a disallowed source directory. It produces one `ARCH015`. | Restaurant: a Chef belongs in the kitchen, not in the pantry office. | Keep. This is a rare case where the physical-location metaphor explains the rule well. |
| `Features/Example.StructuralDeclarationMatchers` | Demonstrates a drop-in structural rule: a request with the named `PizzaId` property and tenant field must implement `IPizzaProvider`. It produces one `ARCH019`. | Pizza request vocabulary: `CreatePizzaRequest`, `PizzaId`, `TenantId`, `IPizzaProvider`. | Keep. It is a focused, coherent mini-domain and shows AND semantics naturally. |
| `Features/Example.WildcardTo` | Demonstrates wildcard layer targets in an allowed dependency rule. It builds cleanly. | Technical global-edge vocabulary: a wildcard is shorthand for matching layer paths. | Technical. Avoid calling it "everyone may use the pantry" unless the actual configuration says that. |

## Documentation Projects

| Project | Primary use case | Suggested teaching vocabulary | Status and review action |
|---|---|---|---|
| `Documentation/Example.DocumentationDemo` | Exercises high-quality generated documentation from a comprehensive XML configuration: descriptions, includes, matchers, rules, site filters, nested and disconnected graphs, and documentation generation. It also intentionally produces one `ARCH002`. | Technical documentation vocabulary. Use domain names only inside the particular documented graph that owns them. | Technical. This is intentionally broad, so its local README should explain it is a documentation stress/example fixture rather than a first-learning example. |
| `Documentation/Example.ReportDemo` | Exercises violation-report generation with one example each of ARCH001 through ARCH005. | Restaurant boundary vocabulary for the individual violations; technical reporting vocabulary for the generated artifact. | Clarify. Keep the reporting narrative separate from the diagnostic stories so it does not become a second, conflicting introduction. |
| `Documentation/Example.VisualStudioSiteDiagnostics` | Clean, passing one-file showcase of every Layer Information and Site Diagnostics marker for the Visual Studio extension. | Technical editor vocabulary: declaration and dependency sites. | Technical. It is a visual capture fixture, so stable source shapes matter more than a metaphor. |

## Scenario Projects

Scenarios are grouped by their parent folder. Every child `.csproj` below is still listed separately because each has a distinct architectural role and can be opened or analyzed independently.

| Project | Primary use case | Suggested teaching vocabulary | Status and review action |
|---|---|---|---|
| `Scenarios/Example.HonestTypeEndpointNames` | Shows that strong endpoint parameter types do not protect convention-based HTTP/model binding when names are misleading, such as `DoctorId patientId`. It produces two `ARCH008` diagnostics. | Identity and endpoint vocabulary: `PatientId`, `DoctorId`, route/model binding. | Keep. This is intentionally framework-independent but the patient/doctor names make the risk obvious. |
| `Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Data` | The allowed data-side project in the package-reference scenario. It demonstrates the project role that may carry the package dependency. | Technical project-boundary vocabulary: data/infrastructure project. | Shared scenario. Its README belongs at the scenario parent and should state that this project is the permitted comparison case. |
| `Scenarios/Example.PackageReferenceBoundaries/Example.PackageReferenceBoundaries.Domain` | The domain-side project that directly references a forbidden NuGet package, producing `ARCH011` even before source code uses a package type. | Technical project-boundary vocabulary: domain project and direct package reference. | Shared scenario. Keep it literal; a restaurant metaphor cannot explain MSBuild package metadata. |
| `Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Application` | The allowed application-side project in a three-project reference boundary scenario. | Technical project-boundary vocabulary: Application role. | Shared scenario. It is the reference-point project for the parent scenario, not a standalone diagnostic story. |
| `Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Domain` | The project that holds an illegal direct project reference and produces `ARCH010`, even when no source file names a type from the forbidden project. | Technical project-boundary vocabulary: Domain must not directly reference Infrastructure. | Shared scenario. This is the central failure project; the parent README should name it explicitly. |
| `Scenarios/Example.ProjectReferenceBoundaries/Example.ProjectReferenceBoundaries.Infrastructure` | The referenced infrastructure-side project that completes the three-project project-reference scenario. | Technical project-boundary vocabulary: Infrastructure role. | Shared scenario. It exists to make the project graph real and should remain minimal. |
| `Scenarios/Example.RepositoryQuerySurface` | Shows a persistence-owned fluent query surface: `OrderRepository` creates `OrderQuery`; outside layers may immediately project it to `OrderProjection` but may not inject or retain the raw query object. It produces two `ARCH001` diagnostics. | Scenario-local order vocabulary: `OrderRepository -> OrderQuery -> OrderProjection`, with `OrderService` as the outside consumer. | Keep. Do not mix it with Waiters/Chefs or the older candy names. The query surface is the actor-like boundary in this scenario. |

## Cross-Catalog Findings

### The example set is functionally broad but needs vocabulary discipline

The examples cover three genuinely different teaching modes:

1. **Architecture traffic:** dependency direction, boundaries, public exposure, and site filters. Restaurant roles work well here.
2. **Declaration semantics:** names, method/property bodies, inheritance, visibility, return policies, and source locations. These need either a small self-contained domain or direct technical terms.
3. **Configuration and tooling:** includes, wildcard files, matcher composition, projects/packages, health reports, documentation, and editor visuals. These should stay technical.

The mistake to avoid is mapping technical terms onto restaurant roles in the same code block. For example, do not describe `Application` as "the Chef" and then show `Application` in the configuration. Either use `Chef` consistently in that example, or describe an application-layer example using application-layer terminology.

### Suggested rewrite priority

The catalog does not justify a blanket rewrite. The following focused passes would yield the most clarity:

1. **Normalize restaurant examples:** `ARCH001`, `ARCH004`, `ARCH005`, `ARCH009`, `ARCH012`, `ARCH016`, and the restaurant-facing ARCH020 examples. Confirm comments and code use only Customer, Waiter, Chef, Pantry, PreparedDish, and Ingredient where appropriate.
2. **Normalize site examples:** make every `Example.AllowedSites` source filename and comment announce its site and whether the edge is allowlisted or blocklisted. Reuse one relationship throughout.
3. **Normalize structural examples:** keep `Pizza*` names only in the structural matcher and observation matcher pair; make their companion contracts and comments say what shape is being matched.
4. **Protect technical examples from accidental metaphors:** project/package references, nested rules, configuration errors, graphs, tooling, and Visual Studio demonstrations should use literal technical language.
5. **Keep scenario-local domains stable:** honest types remain patients/doctors; query surface remains orders/queries/projections. Do not rename them merely to match the introductory restaurant.

### Future review checklist

Before changing any example's actors or comments, complete these steps for that one project:

- [ ] Read its source, configuration, local README, main README link, and integration expectation together.
- [ ] Write its one-sentence lesson in the project README or top-level source comment.
- [ ] Choose exactly one vocabulary from the list above.
- [ ] Rename only the example-specific declarations and comments needed to make that vocabulary consistent.
- [ ] Keep the configuration and expected diagnostic count unchanged unless the task explicitly changes behavior.
- [ ] Run the relevant example integration test and update documentation screenshots only when the visible source changes.

## Proposed Next Decision

Review this document before any mass rename. The recommended next implementation task is a small, reviewable restaurant-vocabulary cleanup of the priority group above, followed by the site example file-organization pass. The technical and scenario-local examples should be left alone unless a specific entry is found to be internally inconsistent.
