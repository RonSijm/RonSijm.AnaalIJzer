---
applyTo: '**'
description: 'Contribution and maintenance rules for RonSijm.AnaalIJzer'
---

# Contributing Guidelines — RonSijm.AnaalIJzer

## Project structure

```
src/
  Main/RonSijm.AnaalIJzer/          — Roslyn analyzer (the shipped artifact)
    Analysis/                        — constructor/dependency analysis logic
    Config/                          — XML config parser and AnalyzerConfig model
    Diagnostics/                     — DiagnosticDescriptors, IDs, code-fix provider
    Matching/                        — pattern matching types (LayerDefinition, PatternMatcher, …)
    Reporting/                       — markdown report and architecture documentation writers
    ArchitecturalLevelAnalyzer.cs    — entry point (thin orchestrator)
  Tests/RonSijm.AnaalIJzer.UnitTests/ — xUnit v3 unit tests
  Tests/RonSijm.AnaalIJzer.IntegrationTests/ — xUnit v3 integration tests that build sample projects
  Tests/RonSijm.AnaalIJzer.Arse.Tests/ — xUnit v3 tests for Arse and its shared tooling host
  Tools/RonSijm.AnaalIJzer.Tooling/   — shared operation catalog and execution host
  Tools/RonSijm.AnaalIJzer.Arse/    — unified RazorConsole and headless command front end
Examples/                            — runnable projects that demonstrate diagnostics and config features
  Directory.Build.props              — shared MSBuild props inherited by every example project
  Diagnostics/Example.Arch00X.*/     — one project per diagnostic case
  Features/Example.<Feature>/        — one project per focused configuration feature
  Documentation/Example.ReportDemo/  — violation report output example
  Documentation/Example.DocumentationDemo/  — generated architecture documentation example
  Scenarios/<ScenarioName>/          — larger examples that need multiple projects
  StarterConfigs/                    — copyable reference XML configuration files
  Assets/                            — images used by documentation
README.md
```

## Code style

- **Tabs, CRLF** for all C# files (follow `.editorconfig`).
- **`internal` by default** — only promote to `public` when consumed across assembly boundaries.
- **No primary constructors** on classes or structs. Records may keep their parameter list.
- **No pass-through interfaces** — do not create `IFoo` unless there are (or imminently will be) ≥2 implementations or the seam is genuinely needed for testing.
- **Feature-based folders** — new files go in the folder whose concern they belong to (`Analysis/`, `Config/`, `Diagnostics/`, `Matching/`, `Reporting/`). Do not add files to the project root unless they are top-level entry points.

## Arse tool modes

- Put tool operations, supported input kinds, validation and execution behavior in `RonSijm.AnaalIJzer.Tooling`.
- Arse's headless command mode and interactive TUI are presentation modes. They must create `ToolRequest` values and execute them through `ToolRunner`; do not duplicate MSBuild loading, config parsing or output generation in either mode.
- Add operations and aliases to `ToolOperationCatalog`. The Arse command list and TUI operation selector must continue to derive from that catalog.
- Update `RonSijm.AnaalIJzer.Arse.Tests` whenever an operation, supported input kind, or Arse component behavior changes.

## Adding or changing a diagnostic

When a new diagnostic (e.g. ARCH004) or a new config element is introduced, **all three of the following must be updated in the same change**:

### 1. Analyzer source

- Add the `DiagnosticDescriptor` to `Diagnostics/ArchitecturalDiagnostics.cs` and its ID to `Diagnostics/ArchitecturalDiagnosticIds.cs`.
- Add parsing logic in `Config/ArchitecturalConfigParser.cs` if a new XML element is required.
- Add detection logic in `Analysis/LayerDependencyAnalyzer.cs`.
- Update `AnalyzerReleases.Unshipped.md` with the new diagnostic ID and severity.

### 2. Tests

- Add at least one unit test in `Tests/RonSijm.AnaalIJzer.UnitTests/` that:
  - Verifies the violation **is** reported on invalid code.
  - Verifies the violation is **not** reported on valid code.
- Use `AwesomeAssertions` for assertions and `xUnit v3` (`[Fact]`) for test methods.
- Do not delete or skip existing tests.

### 3. Example project

- Create diagnostic examples under `Examples/Diagnostics/` named `Example.<DiagnosticId>.<CaseName>/`.
- Create feature examples under `Examples/Features/` named `Example.<FeatureName>/`.
- Create larger multi-project examples under `Examples/Scenarios/<ScenarioName>/`.
- The project must contain:
  - `<ProjectName>.csproj` — normally minimal, relying on `Examples/Directory.Build.props` for shared target framework, analyzer reference and XML `AdditionalFiles`.
  - Either inline `AssemblyMetadata("AnaalIJzerSettings", ...)` settings in `Example.cs` for simple one-file examples, or `ArchitecturalLevels.xml` for broader multi-file, scenario, or include-focused examples — minimal config that triggers exactly this diagnostic.
  - Any extra XML files referenced by `<Include path="..." />`, kept beside the example that uses them.
  - `Example.cs` — one `✅` valid case and one `❌` violating case, with inline comments explaining why.
- Build the example project to confirm it produces the intended ARCH error and nothing else.

## Updating an existing config element

When the behavior or schema of an existing element changes (e.g. a new attribute on `<Class>`, a new child element under `<Forbidden>`):

- Add or update a dedicated feature example project under `Examples/Features/` that clearly demonstrates that one behavior.
- Do not combine distinct feature behaviors in one example project; each sample should make one configuration behavior obvious.
- Use inline `AssemblyMetadata("AnaalIJzerSettings", ...)` settings in `Example.cs` for simple one-file examples. Use `ArchitecturalLevels.xml` when an example has several source files, includes other XML files, or is a broader scenario.
- Update the **README** (see below).

## README

The `README.md` must always be kept in sync with the analyzer's actual behavior.

**Always update the README when:**
- A new diagnostic is added.
- A new config element or attribute is introduced.
- An existing config element or attribute is changed or removed.
- A new example project is added.

**README sections that commonly need updating:**
- `## Configuration reference` — document every supported XML element and attribute, with feature example projects linked inline beside the feature they demonstrate.
- `## Diagnostics` — one sub-section per diagnostic ID with message format, example output, and diagnostic example projects linked inline beside the diagnostic they demonstrate.
- Generated-output sections such as `## Violation report` and `## Architecture documentation` — link their example projects inline beside the feature they demonstrate.

Each sample must use one vocabulary consistently. Diagram nodes, layer names, type names, comments, and explanatory prose must all belong to the same technical or metaphorical model. Do not combine labels such as `Controller / waiter`, or explain an `Application` rule with a kitchen metaphor inside the same sample.

## XML config schema conventions

When extending `ArchitecturalLevels.xml`:

- New matcher attributes (`startsWith`, `contains`, etc.) apply to both `<Class>` and `<Namespace>` elements symmetrically.
- New top-level elements (siblings of `<Layer>`, `<Forbidden>`, `<AllowedDependency>`) must be documented in the README before the change is considered done.
- Top-level elements that reference other files must have a dedicated example project where each referenced file is visible in that example folder.
- Boolean attributes default to `false` unless there is a clear reason to default to `true`. Document the default explicitly in the README.

## Build and test commands

| Task | Command |
|------|---------|
| Build analyzer | `dotnet build src` |
| Run unit tests | `dotnet test src\Tests\RonSijm.AnaalIJzer.UnitTests` |
| Run integration tests | `dotnet test src\Tests\RonSijm.AnaalIJzer.IntegrationTests` |
| Run Arse tests | `dotnet test src\Tests\RonSijm.AnaalIJzer.Arse.Tests` |
| Run all tests | `dotnet test src` |
| Build Arse | `dotnet build src\Tools\RonSijm.AnaalIJzer.Arse` |
| Build a single example | `dotnet build Examples\Diagnostics\Example.Arch001.SkipsLayer` |
| Build all examples | `Get-ChildItem Examples -Recurse -Filter *.csproj \| ForEach-Object { dotnet build $_.FullName }` |

All of the above must pass before a change is considered done.
