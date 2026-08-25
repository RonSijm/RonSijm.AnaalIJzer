# Ground-Up Architecture Review

Date: Monday, August 17, 2026

## Purpose

This review answers a simple question:

If `RonSijm.AnaalIJzer` were started from scratch today, with the feature set and lessons now known, how should it be structured?

This is intentionally not a migration plan. It is a design review of the current system through a ground-up lens, without optimizing for backward compatibility.

## Executive Summary

The repository is no longer a blob. That is the good news.

It already has several strong qualities:

- the feature set is unusually broad for an architecture analyzer;
- examples are treated as real specification assets;
- multiple hosts exist and mostly share behavior instead of inventing their own rules;
- the repo has already moved far away from a single analyzer-centered codebase.

The main problem now is different:

The design still shows the history of how the system grew.

It is better separated than before, but the separation is still not quite the same thing as a clean product architecture. In practice, the repo now has:

- a large number of small `Core.*` projects;
- several hosts that still depend too directly on the analyzer host or its full dependency closure;
- multiple layers of configuration models and projections;
- a test layout that is much improved, but still carries a few historical bucket-style identities;
- a few identity leftovers and source-tree hygiene issues that make the structure feel less intentional than it should.

If I were redrawing this product from zero, I would keep the current capabilities, examples, and mental model, but I would reorganize the implementation into a smaller number of clearer subsystem boundaries:

1. `Language`
2. `Engine`
3. `Application`
4. `Graphs`
5. `Hosts`
6. `Testing`

That is the center of this report.

## What Is Already Strong

Before criticizing anything, it is worth being fair about what is working.

### 1. The product scope is coherent

This is not a random analyzer anymore. It is a real architectural tooling product with:

- analyzer diagnostics;
- example-driven documentation;
- workspace inspection;
- report generation;
- graphing;
- configuration editing;
- a terminal host;
- a WPF host;
- a Visual Studio companion.

That coherence matters. The product has a real identity now.

### 2. The examples are doing real architectural work

The `Examples` tree is one of the healthiest parts of the repo. It does not just demonstrate syntax. It acts as executable specification.

That should absolutely survive any redesign.

### 3. Feature areas are visible

The split into things like:

- dependency rules
- name rules
- visibility
- source locations
- project architecture
- outputs
- graphing

is directionally correct.

The issue is not that the repo lacks boundaries. The issue is that some boundaries are too fine, some are still historical, and some are placed one level too low.

## Findings

## 1. There are too many product-level assemblies for the current size of the core

The `src/Main` tree contains a very large number of `Core.*` projects:

- `Core.ApiSurface`
- `Core.BuildMetadata`
- `Core.Configuration.Compilation`
- `Core.Configuration.Document`
- `Core.Contracts`
- `Core.DependencyRules`
- `Core.Editor`
- `Core.EntryPoints`
- `Core.Exceptions`
- `Core.Findings`
- `Core.Indicators`
- `Core.LayerModel`
- `Core.Matchers`
- `Core.NameRules`
- `Core.Observations`
- `Core.PolicyEvaluation`
- `Core.ProjectArchitecture`
- `Core.RuntimeConfig`
- `Core.SourceLocations`
- `Core.Violations`
- `Core.Visibility`

That is a sign of real refactoring effort, but it is also a sign that the project graph is still too close to implementation categories rather than stable product subsystems.

### Why this is a problem

- it increases packaging complexity;
- it increases IDE/solution noise;
- it makes host projects reference long dependency lists;
- it makes it harder to answer "where does feature X live?" without already knowing the architecture;
- it risks turning every internal concept into a public project boundary.

### Ground-up correction

Collapse these into a smaller number of stable subsystem libraries.

I would strongly prefer something closer to:

- `RonSijm.AnaalIJzer.Language`
- `RonSijm.AnaalIJzer.Engine`
- `RonSijm.AnaalIJzer.Application`
- `RonSijm.AnaalIJzer.Graphs`
- `RonSijm.AnaalIJzer.EditorAbstractions`

with internal folders for feature families rather than project-per-family.

The current project count feels more like "we extracted everything that could be extracted" than "we stopped at the best architectural seam."

## 2. The analyzer host still acts too much like the real product kernel

Several non-analyzer projects still depend on the analyzer project directly or on a shape that mirrors it too closely.

Examples:

- [src/Main/RonSijm.AnaalIJzer.Workspace/RonSijm.AnaalIJzer.Workspace.csproj](../src/Main/RonSijm.AnaalIJzer.Workspace/RonSijm.AnaalIJzer.Workspace.csproj)
- [src/Main/RonSijm.AnaalIJzer.Outputs/RonSijm.AnaalIJzer.Outputs.csproj](../src/Main/RonSijm.AnaalIJzer.Outputs/RonSijm.AnaalIJzer.Outputs.csproj)
- [src/Main/RonSijm.AnaalIJzer.Application/RonSijm.AnaalIJzer.Application.csproj](../src/Main/RonSijm.AnaalIJzer.Application/RonSijm.AnaalIJzer.Application.csproj)
- [src/Main/RonSijm.AnaalIJzer.EditorRuntime/RonSijm.AnaalIJzer.EditorRuntime.csproj](../src/Main/RonSijm.AnaalIJzer.EditorRuntime/RonSijm.AnaalIJzer.EditorRuntime.csproj)

### Why this is a problem

The analyzer should be one host.

It should not be the thing other hosts quietly orbit around.

If a workspace service, documentation generator, TUI, or Visual Studio editor needs to inspect architecture, it should depend on shared application services and engine contracts, not on the analyzer entry project.

### Ground-up correction

Introduce a true application boundary:

- `InspectProjectArchitecture`
- `InspectSolutionArchitecture`
- `GenerateViolationReport`
- `GenerateArchitectureHealth`
- `GenerateArchitectureDocumentation`
- `LoadArchitectureConfiguration`
- `EditArchitectureConfiguration`
- `BuildArchitectureGraph`
- `ExportArchitectureGraphImage`
- `InferArchitectureConfiguration`

Then make:

- analyzer host;
- Arse;
- WPF graph editor;
- Visual Studio extension;
- future CI/GitHub Action host

all call those use cases.

That would make the analyzer a thin adapter instead of a hidden center of gravity.

## 3. The configuration pipeline is still split across too many concept layers

Right now the configuration story is better than it used to be, but it still carries several concept stacks:

- source discovery;
- document loading;
- inline metadata loading;
- compilation parsing;
- runtime config;
- configuration editing;
- graph snapshot loading;
- documentation projection.

Those are all legitimate concerns, but they should be arranged as one obvious pipeline, not as several neighboring models that each know part of the truth.

### Ground-up correction

I would explicitly model configuration as four stages:

1. `AuthoredConfigSource`
2. `AuthoredConfigDocument`
3. `BoundArchitecturePolicy`
4. `EvaluatableArchitectureModel`

Then everything else becomes a projection of one of those layers:

- editing works on `AuthoredConfigDocument`;
- XSD validation works on `AuthoredConfigDocument`;
- docs/diagrams can use `AuthoredConfigDocument` plus optional evidence;
- evaluation uses `EvaluatableArchitectureModel`;
- code fixes and explanations can map findings back through source spans.

That would remove a lot of "this thing is almost the right model, except for..."

## 4. Rule families should be first-class architectural units

The current features are already moving in this direction, but I would push it further.

Today the product is still described a little too much by diagnostics and feature accumulation order.

Ground-up, I would define the engine around rule families:

### A. Boundary Rules

- layer dependencies
- wrong direction
- same-layer dependency
- recognized dependency requirements
- site filters
- nested boundary gates

### B. Type Policy Rules

- allowed types
- forbidden types
- visibility constraints
- contract purity
- transitive exposure

### C. Name Rules

- variable name matching
- declaration name matching
- future subject/name rules

### D. Topology Rules

- project references
- package references
- observed cycles

### E. Location Rules

- source-location ownership
- boundary entry points

### F. Exception Governance

- exception aging
- review windows
- ratcheting or approval policies

### G. Configuration Validation

- invalid matchers
- invalid edges
- invalid includes
- invalid XSD or schema-level issues

Diagnostics should be outputs of these families, not the main organizing idea.

## 5. The Visual Studio architecture is still more clever than it should need to be

[src/Main/RonSijm.AnaalIJzer.EditorRuntime/RonSijm.AnaalIJzer.EditorRuntime.csproj](../src/Main/RonSijm.AnaalIJzer.EditorRuntime/RonSijm.AnaalIJzer.EditorRuntime.csproj) is the shared editor/runtime layer now being separated from the VSIX shell.

That is understandable, but it is also a sign that the host architecture is not yet clean enough.

### Why this is a problem

The editor/VSIX runtime should not need to mirror most analyzer dependencies just to stay safe.

### Ground-up correction

Create one host-neutral editor/runtime library that contains:

- snapshot creation;
- indicator classification;
- quick info content;
- graph tool-window view models;
- settings model abstractions.

Then:

- the VSIX consumes that runtime library;
- the analyzer NuGet consumes the engine and diagnostics mapping;
- neither host needs to pretend to be the other.

In other words: split shared editor intelligence from analyzer intelligence more aggressively.

## 6. Graph rendering and graph editing are still too close together

[src/Main/RonSijm.AnaalIJzer.Graphing.Wpf/RonSijm.AnaalIJzer.Graphing.Wpf.csproj](../src/Main/RonSijm.AnaalIJzer.Graphing.Wpf/RonSijm.AnaalIJzer.Graphing.Wpf.csproj) currently references:

- `ConfigurationEditing`
- `GraphModel`
- `Graphing`

That means the visual graph layer still participates in both rendering and editing orchestration.

### Ground-up correction

Split graph concerns into three layers:

1. `GraphModel`
   - nodes
   - edges
   - groups
   - layout metadata
   - source mapping

2. `GraphApplication`
   - add layer
   - delete layer
   - add edge
   - remove edge
   - persist changes
   - layout commands

3. `GraphRendering.Wpf`
   - Nodify adapters
   - theme
   - controls
   - input bindings
   - export visuals

That way the WPF layer becomes a renderer and interaction shell, not part renderer, part editor, part persistence bridge.

## 7. Tooling is acting like an application layer, but it is not named or structured that way

[src/Main/RonSijm.AnaalIJzer.Application/RonSijm.AnaalIJzer.Application.csproj](../src/Main/RonSijm.AnaalIJzer.Application/RonSijm.AnaalIJzer.Application.csproj) is effectively the shared orchestration layer for:

- workspace analysis;
- outputs;
- configuration editing;
- graph snapshots;
- inspection;
- evidence generation.

That is a good role.

It just should not be called "Tooling" if it is actually the central application/use-case layer.

### Ground-up correction

Rename the concept, not just the project:

- `RonSijm.AnaalIJzer.Application`

Then let Arse, the WPF app, VSIX commands, and future CI integration use that layer.

The current name makes it sound optional or host-specific, even though it is really the shared orchestration core for non-analyzer behavior.

## 8. The test structure is improved, but not yet honest all the way through

The repo now has many focused test projects, which is great.

But it still has a few test identities that read as historical rather than architectural, plus the shared
[`Analyzer.Tests/Testing`](../src/Tests/RonSijm.AnaalIJzer.Analyzer.Tests/Testing)
test helper area.

### Ground-up correction

I would use only three test shapes:

1. subsystem unit tests
2. example/spec integration tests
3. host integration tests

I would not keep generic bucket-style test identities at all.

If a test belongs to dependency rules, it lives with dependency rules.
If it belongs to name rules, it lives with name rules.
If it belongs to documentation output, it lives with outputs.

The current setup is on the way there, but not finished.

## 9. There are still identity leftovers and source-tree ghosts

Examples:

- renamed feature areas can leave behind empty artifact-only directories after larger moves
- build output under source-owned project trees can pollute `rg` and folder scans
- temporary verification artifacts can make the tree feel less intentional than it really is
- search results become less trustworthy when generated files are visually mixed with live source

### Why this matters

This is not just aesthetic.

It makes architectural inspection harder because the source tree stops being trustworthy at a glance.

### Ground-up correction

- no leftover renamed-product directories;
- no generated artifacts under source-owned project trees unless unavoidable;
- no nested ghost root paths;
- stricter repo hygiene around search noise.

This is secondary to architecture, but it affects maintainability every day.

## 10. Multi-targeting should be pushed to the edge

Several projects carry cross-targeting or target-specific project reference gymnastics because the repo supports:

- analyzer packaging;
- `net472` Visual Studio/WPF hosts;
- modern .NET tooling hosts.

That is real, but ground-up I would isolate it better.

### Ground-up correction

- pure language/engine/application libraries should target one neutral runtime where possible;
- host adapters should handle runtime-specific bindings;
- only projects that truly need `net472` should target it;
- only projects that truly need WPF should carry WPF concerns.

The current project graph still leaks host runtime constraints inward more than ideal.

## Recommended Ground-Up Structure

If I were redrawing the repo from zero, I would aim for something closer to this:

```text
src/
  Language/
    RonSijm.AnaalIJzer.Language

  Engine/
    RonSijm.AnaalIJzer.Engine
    RonSijm.AnaalIJzer.Diagnostics

  Application/
    RonSijm.AnaalIJzer.Application

  Graphs/
    RonSijm.AnaalIJzer.GraphModel
    RonSijm.AnaalIJzer.GraphApplication
    RonSijm.AnaalIJzer.GraphRendering.Wpf

  Editor/
    RonSijm.AnaalIJzer.EditorRuntime

  Hosts/
    RonSijm.AnaalIJzer.Engine
    RonSijm.AnaalIJzer.Arse
    RonSijm.AnaalIJzer.GraphEditor.Standalone
    RonSijm.AnaalIJzer.VisualStudio

  Testing/
    subsystem tests
    host tests
    example integration tests
```

## Responsibilities In That Model

### Language

- `.anl` loading
- inline metadata source loading
- include resolution
- XSD validation
- authored document model
- serialization
- edit-safe source mapping

### Engine

- layer classification
- matcher evaluation
- observed facts model
- rule-family execution
- findings
- diagnostic mapping

### Application

- inspect solution/project
- generate config
- merge/split config
- generate docs
- generate reports
- graph projection
- evidence collection
- export image
- configuration editing orchestration

### Graphs

- graph node/edge/group model
- graph editing commands
- layout
- WPF/Nodify rendering

### EditorRuntime

- editor snapshot services
- indicator payloads
- quick info models
- layer/site presentation contracts

### Hosts

- Roslyn analyzer registration
- Arse command surface
- WPF app shell
- VSIX shell

## How I Would Group Inspections

If starting over, I would not organize implementation primarily around `ARCH001`, `ARCH002`, and so on.

I would organize it around inspectors with shared subject models:

### Dependency Inspector Family

- dependency edge existence
- same-layer rules
- reverse-edge rules
- allowed/blocked site rules
- boundary gate evaluation
- recognized dependency requirements

### Type Policy Inspector Family

- allowed type policies
- forbidden type policies
- scoped type policies
- visibility
- contract purity
- transitive exposure

### Naming Inspector Family

- declaration name matches type
- variable/property/parameter name matches type
- future name equivalence or alias rules

### Architecture Topology Inspector Family

- project references
- package references
- observed cycles

### Placement Inspector Family

- source locations
- boundary entry points

### Exception Governance Inspector Family

- review age
- policy drift
- ratcheting

This would reduce special-case logic and make future features easier to place correctly.

## What I Would Keep Exactly

Not everything should change.

I would keep these decisions:

- `Architecture.anl` as the primary persisted format
- inline `AssemblyMetadata("AnaalIJzerSettings", ...)` for tiny examples only
- example projects as first-class acceptance/spec assets
- diagram/report generation as first-class product capabilities
- site-aware dependency analysis
- nested layer boundaries
- name rules as a real subsystem, not a toy add-on

Those choices all feel right.

## Short Version

If written from scratch today:

- fewer assemblies;
- clearer subsystem boundaries;
- one explicit application layer;
- analyzer treated as a host, not the hidden center;
- rule families grouped by semantics, not history;
- graph editing separated from graph rendering;
- no generic bucket-style test projects;
- stricter repo hygiene around leftovers and generated noise.

The current repository is already much closer to that than it used to be.

What it still shows is not lack of design.

It shows design accumulated in stages.

The next architectural improvement is not "more extraction."

It is "simplify upward" by collapsing implementation-level seams into a smaller number of product-level seams.
