# AnaalIjzer Limitation Remediation Roadmap

Status: proposed

## Purpose

This roadmap turns the verified limitations into a deliberate delivery sequence. It does not assume every limitation should become a Roslyn analyzer rule. Each item is assigned to one of three destinations:

| Destination | Use it for |
|---|---|
| Analyzer and shared engine | Deterministic facts available from C# syntax, symbols, semantic operations, or MSBuild project metadata. |
| Workspace and application tooling | Facts that require multiple projects, generated metadata, or a solution-wide view. |
| External evidence integration | Runtime, infrastructure, wire-contract, or non-C# facts produced by another tool. |

The order is intentional. Later features must reuse the earlier semantic-operation and workspace foundations instead of adding another independent parser, matcher, or report path.

## Product Principles

1. Keep the analyzer deterministic, fast, and side-effect free. A normal build must not need network access, runtime startup, or a solution-wide cache.
2. Use Roslyn symbols and `IOperation` values for semantic rules. Do not match source text when aliases, overloads, generated syntax, or fully qualified names can change the spelling.
3. Keep configuration declarative and fail closed. Invalid matchers, ambiguous selectors, or unsupported combinations produce ARCH006 and do not silently broaden a permission.
4. Add one configuration model, one parser, one evaluator, and one documentation projection per feature. Arse, the WPF editor, and the Visual Studio extension consume that shared model rather than reimplementing semantics.
5. Preserve the distinction between a declaration selector, an operation selector, and a value-flow rule. They answer different questions and must not be overloaded into one XML element.
6. Every released rule has an intentionally broken example, a valid counterpart, focused unit tests, MSBuildWorkspace integration coverage, documentation, graph/editor representation where applicable, and a fixer only when the repair is deterministic.
7. Do not infer business intent. Helpful configuration generation may propose rules with confidence information, but never enables a policy or exception by itself.

## Delivery Order

| Phase | Outcome | Depends on |
|---:|---|---|
| 0 | Remove schema drift and add package-consumer verification | None |
| 1 | Reusable semantic operation and member matcher foundation | Existing matcher and configuration infrastructure |
| 2 | Member-access and invocation policies, including selected BCL members | Phase 1 |
| 3 | Precise project-edge selectors and solution topology enforcement | Existing workspace host; Phase 0 for config hygiene |
| 4 | Complete direct NameRules coverage and bounded intraprocedural provenance | Phase 1 |
| 5 | Declarative behavioral operation policies | Phases 1 and 4 |
| 6 | Explicit operation ownership and contract correlation | Phase 3 and Phase 5 |
| 7 | API metadata and generated-code opt-in | Phases 1, 3, and 5 |
| 8 | Assembly/package-origin architecture and external evidence adapters | Phase 3 |

Phases 0 through 4 are the recommended first product milestone. Phases 5 through 8 should each be independently approved after their design spike, because their configuration cost and false-positive risk are substantially higher.

## Phase 0: Configuration And Release Hygiene

### Goals

- Make `src/Main/RonSijm.AnaalIJzer/Scheme/AnaalIjzer.xsd` the single authoritative schema source.
- Prevent the repository-root `AnaalIjzer.xsd` from silently drifting.
- Verify that the packed `0.3.x` analyzer behaves as a consumer sees it.

### Implementation Steps

1. Decide whether the root schema is a supported public artifact.
   - If no: remove it and replace references with the authoritative schema path or an explicitly copied configuration-local schema.
   - If yes: generate it from the authoritative schema during the build; do not edit two files by hand.
2. Add a deterministic schema-sync target or script that copies bytes from the authoritative source to every supported distribution location.
3. Add a test that fails when an expected generated schema copy differs from its source. Normalize neither XML nor whitespace: an intentionally generated copy should be byte-identical.
4. Add a package-consumer integration test:
   - run `dotnet pack` into a temporary local feed;
   - create or restore a minimal consumer project against that feed;
   - add `Architecture.anl` as an explicit `AdditionalFiles` item;
   - compile a known valid and known invalid source;
   - assert the expected analyzer diagnostic is emitted from the package, not from a project reference.
5. Run the package-consumer test on Windows and Linux CI to protect the analyzer package layout and schema distribution.

### Definition Of Done

- Only one editable XSD remains.
- A clean local package produces analyzer diagnostics in an independent consumer project.
- No release version claim is inferred from a search-engine cache; CI records the packed package version and artifact name.

## Phase 1: Semantic Operation Foundation

### Why This Comes First

Selected-member restrictions, return-value expansion, behavioral rules, and stronger NameRules all need the same reliable view of C# operations. Building each feature from `SyntaxNode` switches would recreate the current duplication problem.

### Target Model

Add a small shared semantic-operation model in a focused library, for example `RonSijm.AnaalIJzer.Core.SemanticOperations`. It must not depend on WPF, Visual Studio SDK APIs, or workspace hosting.

The model should represent:

- operation kind: invocation, property read, property write, field read, field write, event access, object creation, conversion, assignment, return, and argument;
- containing type and selected member symbols;
- instance versus static access;
- generic method construction and selected overload;
- source location, caller symbol, caller layer, and existing architectural site;
- a stable semantic display string for diagnostics, documentation, and editor hover text.

### Configuration Shape

Introduce a generic nested semantic matcher rather than one-off attributes on every future policy:

```xml
<OperationMatcher kind="Invocation">
  <ContainingType exactName="System.DateTime" />
  <Member exactName="UtcNow" memberKind="Property" />
</OperationMatcher>
```

Rules own the matcher and define whether it is required or forbidden. Matcher child elements use existing composable matcher semantics where applicable. Conditions on one matcher are ANDed; sibling matchers are ORed. Unknown operation kinds, incompatible member kinds, or impossible attribute combinations are ARCH006 errors.

### Implementation Steps

1. Inventory current syntax-site analysis and return-value matching. Replace duplicated symbol extraction only after characterization tests capture existing behavior.
2. Create a Roslyn-to-model adapter that converts supported `IOperation` values into the shared model.
3. Create matcher types and evaluators with no analyzer diagnostic dependency.
4. Make the parser, XSD, configuration document model, documentation renderer, graph model, and configuration editor preserve operation matchers without interpreting them differently.
5. Add semantic unit tests for aliases, fully qualified references, extension methods, overloads, generic methods, property accessors, static/instance distinctions, nullable wrappers, and unresolved/error symbols.
6. Add a compact editor snapshot projection so Visual Studio can display the matched operation and reason without duplicating analysis.

### Definition Of Done

- One shared symbol/operation matcher is used by all new operation-based policies.
- No new policy reads identifiers purely as text when a semantic symbol is available.
- Existing dependency-site and return-policy tests continue to pass unchanged.

## Phase 2: Selected Member And Invocation Policies

### Goals

Close the highest-value gap: restrict selected APIs within selected caller layers while allowing other members of the same type.

### Proposed Configuration

Use a general policy rather than a BCL-specific feature:

```xml
<ForbiddenOperations>
  <ForbiddenOperation inLayer="Application" allowedSites="StaticMember">
    <OperationMatcher kind="PropertyRead">
      <ContainingType exactName="System.DateTime" />
      <Member exactName="UtcNow" memberKind="Property" />
    </OperationMatcher>
    <Description>Application code receives time through IClock.</Description>
  </ForbiddenOperation>
</ForbiddenOperations>
```

Support `ForbiddenOperation` first. Add `RequiredOperation` only in Phase 5 because "some required call occurs" has different ordering and control-flow semantics.

### Semantics

- Match the resolved member symbol, not source spelling.
- Support static and instance operations explicitly.
- Treat an invoked property accessor as property access, not an arbitrary method invocation.
- Include `DateTime` and other Roslyn special types in this policy only. Do not remove the existing special-type exclusion from broad layer dependency classification.
- Permit optional caller-layer selectors, ordinary architectural site filters, and existing exception metadata rules.
- Report a new diagnostic ID only after the configuration contract and message are stable. The diagnostic must name the caller layer, operation, configured policy, and matched source site.

### Examples And Tests

Create focused examples for:

- `DateTime.UtcNow`, `DateTime.Now`, and `DateTime.Today` forbidden outside a clock adapter;
- `Task.Result` and `Task.Wait` forbidden in an async application layer;
- `IServiceProvider.GetService` allowed in a composition root but forbidden elsewhere;
- a selected `Environment` member forbidden while another member remains allowed;
- aliases and fully qualified calls resolving identically.

Add unit tests for every operation kind initially supported, every relevant `DependencySites` filter, nested layer boundaries, exceptions, code fixes where configured replacements are available, documentation, graph editing, and Visual Studio labels.

### Tooling Work

- Arse: validate, inspect, document, merge, split, and generate a report containing matched operation evidence.
- WPF graph editor: show operation-policy chips on affected layer nodes and allow editing the matcher tree.
- Visual Studio: show the existing site information plus policy status and a QuickInfo explanation; no new visual noise by default.

### Definition Of Done

- A configuration can forbid one member without forbidding its containing type.
- Direct BCL clock calls are enforceable without an AnaalIJzer dependency on another framework package.
- Normal layer matching remains unchanged for special types.

## Phase 3: Project Selectors And Solution Topology

### Part A: Precise Project Edges

Extend project-edge rules with optional nested source and target selectors:

```xml
<AllowedProjectReference from="Application" to="Contracts">
  <From exactName="Exquise.Patient.Application" />
  <To exactName="Exquise.Patient.Contracts" />
</AllowedProjectReference>
```

The `from` and `to` attributes retain group-level behavior. Nested selectors narrow an already matched group edge; they never replace project-group classification. This preserves broad reporting groups while supporting precise exceptions.

Implementation:

1. Add a `ProjectReferenceRule` model containing source/target group names and optional `ProjectMatcher` collections.
2. Reuse the generic matcher engine for project names; do not create a second ad hoc exact-name parser.
3. Evaluate blocked rules before allowed rules. A rule with selectors applies only when both group and project selector match.
4. Update `ARCH010` messages, documentation, configuration fixes, graph labels, the WPF editor, and Visual Studio graph editing.
5. Test group-only regressions, narrow permissions, selector precedence, wildcard groups, and include/merge/split preservation.

### Part B: Enforceable Solution Topology

The solution inspection path already aggregates observed cycles. Build on it rather than duplicating it in the compiler analyzer.

1. Introduce an explicit `SolutionTopology` configuration section, separate from per-project `ProjectArchitecture`.
2. Allow projects to be aggregated into a named logical module by one or more project matchers. Do not infer modules from namespaces or project names.
3. Generate a solution graph with project edges, configured module edges, observed layer edges, and evidence locations.
4. Add `arse inspect --solution --enforce-topology` or a dedicated `arse validate --solution` command that exits nonzero for configured solution rules.
5. Add a GitHub Action entry point that runs the command after restore/build, publishing Markdown and JSON evidence artifacts.
6. Keep compiler `ARCH018` project-scoped. Give solution findings their own stable report IDs or a clear non-compiler diagnostic namespace, rather than pretending they are ordinary compiler diagnostics.

### Definition Of Done

- A broad `Application -> Contracts` group relationship can have a one-project exception without singleton groups.
- A configured logical module cycle is detected in a solution command and yields navigable evidence.
- A normal standalone project build remains self-contained.

## Phase 4: NameRules And Direct Provenance

### Goals

Broaden correct handling of direct C# forms before attempting whole-program flow analysis.

### Stage 4A: Complete Direct Language Forms

Add explicit support for:

- compound assignments where the semantics are meaningful for a configured rule;
- deconstruction assignments with a component-by-component source/target model;
- expression-bodied property and indexer getters;
- expression-bodied lambdas and block lambdas;
- local functions with the correct owning symbol;
- conditional, coalesce, tuple, conversion, and null-forgiving wrappers;
- named and optional arguments;
- `ref`, `out`, and `in` arguments with an explicit policy decision per modifier.

Use the Phase 1 operation model to identify source and target symbols. Do not report an outer method's return rule for a nested lambda or local-function return.

### Stage 4B: Bounded Intraprocedural Tracking

Add opt-in configuration, for example `valueTracking="Direct|IntraProcedural"`.

- `Direct` stays the default and preserves current speed and predictability.
- `IntraProcedural` builds a Roslyn `ControlFlowGraph` for one method, accessor, constructor, or lambda.
- Track only explicit local aliases and assignments with known source symbols.
- Merge branch states conservatively: if provenance is ambiguous, do not manufacture a violation; record an optional inspection note if useful.
- Do not cross method calls, virtual dispatch, collections, delegates, or reflection in this phase.

### Stage 4C: Deferred Interprocedural Work

Do not schedule full taint analysis as part of this roadmap milestone. First collect practical examples that cannot be addressed by stages 4A and 4B. Any later summary-based interprocedural analysis must be opt-in, cancellation-aware, and independently benchmarked.

### Tests And Examples

Add one focused example project per language shape and one integration project exercising the same configuration through `AssemblyMetadata` where inline settings are appropriate. Test valid/invalid code, renamed identifiers, aliases, nested functions, and code-fix behavior only where a deterministic rename is configured.

### Definition Of Done

- NameRules has explicit, documented coverage rather than accidental behavior for modern syntax.
- Direct mode remains fast and backward-compatible.
- Intraprocedural mode is bounded, opt-in, and has benchmark coverage on large methods.

## Phase 5: Behavioral Operation Policies

### Goal

Make narrow, mechanically provable behavioral checks available without claiming to prove business behavior.

### Policy Families

Build on Phase 1 operation matchers and Phase 4 control-flow support:

- `ForbiddenOperation`: already introduced in Phase 2;
- `RequiredOperation`: at least one matching operation must occur in a selected declaration;
- `RequiredOperationBefore`: a matching operation must dominate a configured mutation or publication operation;
- `ForbiddenOperationAfter`: prevent a configured call after a terminal operation;
- `MaximumOperationCount`: limit a selected operation per method or accessor.

Each rule needs a selected declaration scope, caller-layer selector, and operation matchers. It must state whether matching is lexical order or control-flow dominance. Default to the stronger and more explicit control-flow interpretation only when a CFG is available.

### Non-Goals

- Do not infer side effects from interface names.
- Do not claim a validator is semantically correct because it was invoked.
- Do not inspect runtime behavior, asynchronous continuation execution, reflection, or generated code unless separately configured.

### Examples

- a query layer cannot invoke configured persistence mutation members;
- a validator must execute before a selected mutation call;
- a message must be sent through the configured publisher method;
- a domain operation must not invoke `DateTime.UtcNow` directly.

### Definition Of Done

- Rules have explicit operation and ordering semantics.
- Diagnostics explain which required or forbidden operation caused the result.
- Graphs and documentation show behavioral rules as policies, not ordinary dependency edges.

## Phase 6: Operation Ownership And Contract Correlation

### Goal

Offer an opt-in, explicit model for teams that want to connect entry points, application owners, and contracts without hard-coding ASP.NET or a naming convention into the analyzer.

### Configuration Model

Add an `Operations` section where each operation is named by the configuration author and has:

- exactly one owner selector;
- optional request and response type selectors;
- zero or more entry-point selectors;
- optional allowed host layers;
- descriptions and exception metadata.

Example shape:

```xml
<Operation name="ChangePatientStatus">
  <Owner>
    <Method exactName="ChangePatientStatus" />
  </Owner>
  <Request>
    <Class exactName="ChangePatientStatusRequest" />
  </Request>
  <EntryPoint>
    <Method exactName="ChangePatientStatus" />
  </EntryPoint>
</Operation>
```

This is a generic symbol model. Route, controller, queue, and scheduled-job conventions can be optional adapters later.

### Enforcement Split

- The analyzer enforces local facts: an owner declaration has the required shape, an entry point invokes its configured owner, and local contracts match selectors.
- Workspace tooling enforces global facts: one owner across the solution, no duplicate ownership, and every declared entry point maps to an owner.
- Human review still chooses the operations and their owners.

### Definition Of Done

- The feature never attempts to infer business operations from suffixes alone.
- A solution report can explain every operation, owner, entry point, and uncovered entry point.

## Phase 7: API Metadata And Generated Code

### Part A: API Metadata Evidence

Do not make the analyzer depend on ASP.NET, OpenAPI, or a serializer package. Define an external, versioned JSON evidence contract containing generated API metadata: endpoints, routes, request/response types, serializer names, and relevant attributes.

1. Add a small `ApiMetadataEvidence` model and schema in a shared tooling project.
2. Add `arse inspect --api-metadata <file>` to combine evidence with C# API-surface policies.
3. Start with comparison/report findings, not build diagnostics.
4. Provide a sample producer for `System.Text.Json` and an optional ASP.NET/OpenAPI producer in a separate integration project.
5. Make unknown or incomplete evidence visible in the report rather than treating absence as proof.

### Part B: Generated Code Opt-In

Add a root configuration mode such as `generatedCode="Exclude|IncludeConfigured|IncludeAll"` and optional path matchers.

Because Roslyn's generated-code registration flag is global, configure the analyzer to receive generated callbacks and centralize the decision in one `ShouldAnalyzeTree` gate. The default must retain today's no-generated-code behavior for every analyzer, editor, workspace collector, and report.

Implementation steps:

1. Centralize generated-code classification in `Core.Observations`.
2. Apply the same configuration evaluator in engine, workspace, editor runtime, and report generation.
3. Add safety caps for generated document size and diagnostic count.
4. Mark diagnostics clearly as generated so a code fix can direct the user to the generator/template rather than offering unsafe edits.
5. Test source-generated, design-time generated, hand-authored `*.g.cs`, and configured path cases.

### Definition Of Done

- Runtime API metadata remains an optional evidence source.
- Generated-code analysis has an explicit owner, scope, and noise budget.

## Phase 8: Package Origin And External Evidence Integrations

### Package And Assembly Origin

Deliver in two bounded stages:

1. **Reference inventory:** extend the workspace manifest to record raw MSBuild `Reference` and `HintPath` assembly references alongside project and package references. Add policies that can allow or forbid these by assembly identity.
2. **Architecture origin metadata:** define an opt-in package manifest, for example `AnaalIjzer.package.json`, containing package ID, assembly identities, logical module, and architectural roles. The workspace resolver loads it only from explicitly trusted packages and reports missing/ambiguous origins.

Do not assume one package equals one architecture role. A package with multiple assemblies must declare each assembly independently.

### External Evidence Integration

Create a generic `ArchitectureEvidence` JSON envelope with:

- producer name and version;
- evidence kind;
- source artifact path and timestamp;
- normalized subjects and relationships;
- locations where a host tool can navigate;
- confidence and completeness declarations.

Arse should load multiple evidence files and add their findings to the architecture health report. The WPF and Visual Studio tools should display evidence as read-only annotations unless a known configuration editor exists.

Initial adapters should be small and independent:

- DI container validation results;
- OpenAPI/serialization snapshots;
- infrastructure or repository-linter findings;
- test-result summaries for persistence or deployment checks.

The core analyzer must not parse Bicep, SQL, YAML, or runtime containers directly.

### Definition Of Done

- Assembly references have a clear policy story.
- External evidence is additive, versioned, and cannot silently masquerade as compiler proof.

## Cross-Cutting Work Required In Every Phase

For every new rule family:

1. Reserve a diagnostic ID only after semantics, message shape, and metadata properties are agreed.
2. Add parser, XSD, configuration-document, merge/split, include, and validation coverage.
3. Add evaluation unit tests for valid, invalid, nested, wildcard, exception, site-filter, and malformed-config cases as applicable.
4. Add a focused example project with one valid and one intentionally invalid scenario. Keep unrelated settings out of the example.
5. Add MSBuildWorkspace integration expectations for the intended diagnostics.
6. Add Arse command support, report evidence, and generated documentation rendering.
7. Add graph model support. Make the WPF and Visual Studio editors preserve and edit the rule even when they do not provide a visual graph primitive for it yet.
8. Add Visual Studio QuickInfo/site status support without making the editor extension a second analyzer.
9. Add code fixes only where configuration supplies an unambiguous replacement or the edit is mechanically safe. Otherwise provide a navigable diagnostic explanation, not a speculative rewrite.
10. Update configuration reference, diagnostic reference, common-use-case documentation, XSD comments, screenshots where applicable, and the feature checklist for a new ARCH diagnostic.
11. Run focused unit tests, integration tests, package-consumer tests where relevant, the cross-platform solution build, Windows-only tool builds, Markdown checks, and `git diff --check`.

## Explicit Non-Goals

The following are valuable architectural controls, but are not promises for the core analyzer roadmap:

- deciding bounded contexts, owners, exceptions, or business meaning;
- proving runtime DI lifetimes, database translation, transaction correctness, HTTP behavior, deployment topology, performance, race freedom, or resource cleanup;
- replacing focused integration, contract, deployment, or performance tests;
- automatic architecture inference that silently changes a team's configured rules.

Those facts should enter AnaalIjzer only through explicit configuration or clearly labelled external evidence.

## Milestone Acceptance Criteria

The first complete milestone, Phases 0 through 4, is ready when:

- schema distribution cannot drift;
- the analyzer package is tested as a real consumer dependency;
- one policy can match a selected semantic member including `DateTime.UtcNow`;
- project edges can be narrowed without proliferating groups;
- solution inspection can enforce configured topology separately from compiler diagnostics;
- NameRules correctly handles the explicitly documented direct forms and optional bounded intraprocedural tracking;
- every capability is documented, demonstrated, testable, and visible in the shared tooling model.
