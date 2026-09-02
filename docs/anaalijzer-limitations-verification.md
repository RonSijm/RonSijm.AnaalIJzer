# AnaalIjzer Limitation Verification

Verified: 2026-09-03

## Scope

This document verifies the claims in the external "AnaalIjzer limitations" review against the current checkout. It is an evidence review, not a feature plan and not an assertion that every suggested capability should be built.

The verdicts use these meanings:

| Verdict | Meaning |
|---|---|
| Confirmed | The current implementation does not provide the claimed capability. |
| Partly confirmed | The limitation exists in one important execution path, but an existing capability narrows it. |
| Not confirmed | The checkout already provides the claimed capability. |
| Needs package comparison | The version is known, but the package contents were not unpacked and compared with this checkout. |

The review traced analyzer registrations, configuration parsing, semantic analysis, workspace inspection, report generation, and current documentation. It did not infer behavior from names or examples alone.

## Verdict At A Glance

| Claim | Verdict | Important correction |
|---|---|---|
| Special .NET member policies | Confirmed | Static access to classifiable non-special types is visible, but `DateTime` is ignored and individual members cannot be selected. |
| General invocation and member-access policies | Confirmed | Return-value rules can match a direct returned member access, but there is no general caller-layer policy for selected members. |
| Rich data flow and provenance | Confirmed | NameRules is syntax-local rather than flow-sensitive or interprocedural. |
| Complete NameRules coverage | Confirmed | The current supported sites are useful, but not all modern expression and assignment forms are covered. |
| Operation-aware contract naming | Confirmed | Current matching is structural; it does not correlate operation names, routes, request types, and response types. |
| Exact operation ownership | Confirmed | There is no model that proves one canonical application owner across HTTP, jobs, queues, and UI. |
| Cross-project observed dependency cycles | Partly confirmed | Compiler diagnostic `ARCH018` is per compilation, but `arse inspect --solution` already reports observed cycles across matching project configurations. |
| Fine-grained project-edge selectors | Confirmed | Project groups are the current selector and require finer groups for narrow exceptions. |
| Assembly/package architectural origin | Partly confirmed | Direct project references and package IDs are inspected; raw assembly references and package-to-architecture origin are not. |
| Behavioral contract selectors | Confirmed | Body observations select declarations, but do not prove general behavior or control flow. |
| Return-value provenance | Confirmed | `ARCH020` recognizes direct expressions only after limited wrapper unwrapping. |
| API surface beyond C# declarations | Confirmed | C# signatures are analysed thoroughly, but serializers, OpenAPI, generators, and wire behavior are not. |
| Generated-code visibility | Confirmed | Generated code is deliberately skipped and has no configuration opt-in. |
| Non-C# and runtime architecture claims | Confirmed | These belong in complementary repository, host, integration, or deployment tooling. |
| Human architecture decisions | Confirmed | Generation can propose a baseline, but cannot decide business intent or approve exceptions. |
| Published package trails the checkout | Not confirmed | `0.3.0` is published and matches this checkout's `VersionPrefix`; the earlier `0.1.4` result was stale search-index data. |
| Root XSD is current | Not confirmed | The root XSD is materially older than the authoritative project schema. |
| ARCH009 through ARCH020 lack documentation | Not confirmed | The README and diagnostic index already describe all twenty diagnostics. |

## Confirmed Implementation Gaps

### Special .NET types and selected members

**Verdict: Confirmed.**

Layer dependency evaluation intentionally ignores Roslyn `SpecialType` symbols before it tries to classify them into a layer. `System.DateTime` is one of those symbols, so a layer or type policy cannot currently see `DateTime.UtcNow`, `DateTime.Now`, or `DateTime.Today` as dependency targets.

There is some existing static-member support: a static method, property, field, or event on a classifiable non-special type is reported as a dependency at `StaticMember`. That can prohibit the containing type as a whole, such as `System.Environment` or `System.Diagnostics.Process`. It does not distinguish one member from another, and it does not cover instance members such as `Task.Result` or `Task.Wait()`.

Relevant implementation:

- `src/Main/RonSijm.AnaalIJzer.Engine/Analysis/BoundaryRules/LayerDependencies/LayerDependencyAnalyzer.Diagnostics.cs`
- `src/Main/RonSijm.AnaalIJzer.Engine/Analysis/BoundaryRules/LayerDependencies/LayerDependencyAnalyzer.Evaluation.TypeReferences.cs`
- `src/Main/RonSijm.AnaalIJzer.Engine/Analysis/BoundaryRules/LayerDependencies/LayerDependencyAnalyzer.SyntaxSites.Expressions.cs`

The suggested semantic `ForbiddenMemberAccess` shape is a reasonable future direction, provided it is based on Roslyn symbols rather than source spelling.

### General invocation and member-access policies

**Verdict: Confirmed.**

There is no rule model that says "a caller in this layer may not invoke this exact method" or "may not read this exact property." Existing structural observations can identify that a declaration contains an invocation, member access, literal, throw, or object creation, but that only selects the declaration for another policy. It is not an invocation-level enforcement model.

That means the review's examples remain out of reach as direct policies: selected `IServiceProvider.GetService` calls, `DbContext.SaveChanges`, `Task.Wait`, or one selected `Environment` member while allowing another member.

`ReturnValuePolicy` is a deliberate narrow exception: it can match a direct returned `MemberAccess` expression. That does not provide a general access rule, nor does it track values away from the return expression.

### Rich data flow and NameRules coverage

**Verdict: Confirmed.**

NameRules handles a useful local set of cases:

- parameter declarations for constructors and methods;
- method return declarations;
- field, property, and local declarations;
- invocation and object-creation arguments;
- simple assignments;
- local, field, and property initializers;
- ordinary `return` statements.

It does not build a control-flow graph or an `IOperation` data-flow graph, and it does not follow values across aliases, branches, collections, helper methods, or method boundaries. The analyzer only registers an assignment callback for `SimpleAssignmentExpression`, not compound assignment or a dedicated deconstruction model.

The review is also directionally right about expression forms. Return-value analysis supports expression-bodied methods and local functions, but not expression-bodied properties. NameRules does not have explicit support for lambda return semantics, and its return handling should be treated as method-oriented rather than a general expression-flow engine.

Relevant implementation:

- `src/Main/RonSijm.AnaalIJzer.Engine/Analysis/NamingRules/LayerDependencyAnalyzer.NameRules.ValueMovement.cs`
- `src/Main/RonSijm.AnaalIJzer.Engine/ArchitecturalLevelAnalyzer.cs`
- `src/Main/RonSijm.AnaalIJzer.Core.NameRules/NameRuleSemanticSubjectResolver.cs`

This is a real extension area. It should begin with precisely defined extra syntax and operation forms, not jump straight to a broad taint-analysis promise.

### Operation-aware contracts and exact ownership

**Verdict: Confirmed.**

Current declaration and body observations can select a type or member based on names, kinds, and the presence of syntax inside a body. They do not correlate an endpoint action, route, command name, request type, response type, hosted entry point, and canonical operation owner.

The current tool can prevent configured shortcuts and enforce a known boundary. It cannot prove that exactly one application owner exists for every business operation or that every host delegates to it. That would need an explicit operation model, annotations, or a manifest; it should not be guessed from suffixes.

### Fine-grained project rules

**Verdict: Confirmed.**

Project architecture currently works by assigning projects to `ProjectGroup` values and allowing or blocking group-to-group relationships. An `Application -> Application` rule therefore applies to every project in that group. A narrow exception currently requires creating narrower groups with exact project matchers.

This is a configuration ergonomics limitation, not a lack of project architecture enforcement. Project-level selectors on an individual rule would preserve broad reporting groups while allowing a narrowly selected edge.

### Assembly and package architecture origin

**Verdict: Partly confirmed.**

`ARCH010` evaluates direct MSBuild `ProjectReference` entries. `ARCH011` evaluates package IDs, including package data resolved from `project.assets.json`; it is not limited to a manually listed direct package name.

What is absent is equally important:

- raw assembly or `HintPath` references are not represented as architectural project edges;
- package assemblies are not mapped back to an AnaalIjzer project group or module of origin;
- a package containing several assemblies cannot express separate architectural roles for each assembly.

The review should therefore say that package ID policy exists, while assembly-origin topology does not.

Relevant implementation:

- `src/Main/RonSijm.AnaalIJzer.Core.Configuration.Compilation/Parsing/ArchitecturalConfigParser.ProjectArchitecture.ReferenceRules.cs`
- `src/Main/RonSijm.AnaalIJzer.Workspace/ProjectAnalysisHost.Configuration.References.cs`

### Behavioral contract selectors

**Verdict: Confirmed.**

`CodeObservationMatcher` supports selected syntax observations such as `Throw`, `Invocation`, `New`, `Identifier`, `MemberAccess`, and `Literal`. The observations are existential: they establish that some matching syntax occurs within a declaration and can then route that declaration into a structural policy, such as an inheritance policy.

They do not prove that validation runs before mutation, that a query has no side effects, that a message was actually published, or that a dependency is used exactly once. Those claims need explicit invocation/control-flow rules and sometimes runtime tests.

### Return-value expression coverage

**Verdict: Confirmed.**

`ARCH020` analyzes a direct `return` expression and expression-bodied methods or local functions. It unwraps only parentheses, casts, `await`, `as`, and null-forgiving expressions before matching the result.

It does not generally follow a sentinel through conditional or coalesce expressions, a local variable, a property getter, a helper method, or an arbitrary invocation. A direct `return DateTime.UtcNow;` can be selected as a `MemberAccess` return pattern, but that narrow ability does not solve general clock-access policy.

### API surface and generated code

**Verdict: Confirmed.**

API-surface policies inspect C# declaration signatures extensively: types, parameters, return types, properties, fields, events, attributes, generic arguments, tuples, delegates, and several wrapper shapes. That is stronger than a basic public-type scan.

They do not inspect runtime serializer metadata, generated OpenAPI, converter behavior, reflection-created endpoints, or wire compatibility. Transitive traversal is bounded by configuration and intentionally stops on selected type shapes.

Generated code is intentionally excluded at analyzer registration via `GeneratedCodeAnalysisFlags.None`; source-location and workspace observation paths follow the same principle. There is no opt-in to inspect generated code today.

## Important Corrections To The Review

### Solution-wide observed cycles already exist in inspection output

**Verdict: Partly confirmed, not wholly missing.**

The core compiler analyzer creates its observed-dependency collector per compilation. Therefore `ARCH018` emitted during an ordinary project build only sees that compilation's source dependency graph.

However, `arse inspect --solution <solution.slnx>` already opens the solution and aggregates observations across projects that share a configuration. Its health report calls the same observed-cycle evaluator with a `Solution` scope. It is a report-level result, not a compiler diagnostic that can fail an individual project build.

The real future opportunity is narrower than the review suggests: decide whether solution-scoped observed cycles should gain CI enforcement, logical-module aggregation, or a dedicated diagnostic/report category. It is not necessary to invent solution inspection from scratch.

Relevant implementation:

- `src/Main/RonSijm.AnaalIJzer.Engine/ArchitecturalLevelAnalyzer.ObservedCycles.cs`
- `src/Main/RonSijm.AnaalIJzer.Application/ArchitectureHealthReportGenerator.Inspection.cs`
- `src/Main/RonSijm.AnaalIJzer.Application/ApplicationOperationCatalog.Operations.cs`

### Documentation already covers ARCH009 through ARCH020

**Verdict: Not confirmed.**

The root README has a complete ARCH001 through ARCH020 legend and detailed diagnostic sections. `docs/diagnostics/index.md` also lists every diagnostic, including ARCH009 through ARCH020. The review may have been based on an older revision or a shorter generated document.

No diagnostic-documentation remediation is needed for that specific claim. The documentation can still be improved over time, but this is not a current coverage gap.

## Boundaries That Belong Outside The Analyzer

The review correctly identifies several areas that should remain complementary tooling rather than be forced into a C# compiler analyzer:

- Bicep, ARM, SQL, XAML, YAML, JSON, Markdown, and other non-C# artifacts;
- completed runtime dependency-injection graphs and lifetime correctness;
- database translation, migrations, transactions, and cache behavior;
- HTTP status codes, headers, serialization, and compatibility behavior;
- deployment topology, identities, networking, and mixed-version rollout safety;
- performance, concurrency, disposal, UI lifecycle, and resource behavior.

AnaalIjzer can contribute evidence from C# source and project metadata, but hosted integration tests, repository checks, contract tests, infrastructure validation, and human review are the right owners for these facts.

## Boundaries That Should Remain Human-Owned

The review is also correct that configuration generation and architecture checks must not pretend to decide business intent. Current helpful/convention generation can propose a baseline from observed code, but it cannot know whether two bounded contexts should be separate, whether a port is meaningful, or whether an exception rationale is acceptable.

`ARCH017` can enforce exception metadata, ownership, and expiry. It cannot approve an exception. That belongs to a human architecture decision.

## Tooling And Distribution Findings

### Published package versus checkout

**Verdict: Not confirmed.**

The NuGet flat-container index lists `RonSijm.AnaalIJzer` version `0.3.0`, matching this checkout's `VersionPrefix` `0.3`. The earlier conclusion that the latest package was `0.1.4` was wrong: it came from stale search-index data rather than the package index.

This does not remove the value of a release verification test. A CI job should pack the repository and validate the produced analyzer package from a clean consumer project, but there is no current evidence of a version-level release gap.

### Repository-root schema

**Verdict: Confirmed.**

The repository contains two schemas:

- `AnaalIJzer.xsd` at the repository root: 31,050 bytes;
- `src/Main/RonSijm.AnaalIJzer/Scheme/AnaalIJzer.xsd`: 56,151 bytes.

The larger project schema is the authoritative current schema and contains many newer configuration elements and attributes missing from the root copy. The root schema is therefore stale and should either be generated from the authoritative file or removed as an independently maintained copy.

## Adjusted Priority Order

The review's priorities are broadly sensible after the corrections above:

1. Semantic selected-member policies, including explicitly supported BCL/special-type symbols.
2. Precise selectors on project-reference rules.
3. A decision about promoting existing solution inspection cycles into enforceable CI policy or logical-module analysis.
4. Carefully scoped NameRules syntax and operation coverage, before any ambitious interprocedural analysis.
5. Explicit operation ownership modelling, only if teams can provide an authoritative manifest or annotations.
6. Generated API metadata and OpenAPI integration, likely shared with contract tooling.
7. Optional generated-code analysis with clear ownership and noise controls.
8. Package/assembly origin metadata for multi-repository architectures.

The stale root XSD is small, high-confidence maintenance work. Package verification is worthwhile release hygiene, but not remediation for a confirmed stale-package problem.
