## Arse TUI

Arse - **A**rchitecture **R**ule **S**tandalone **E**xecutable - can load a real project or solution with `MSBuildWorkspace`, so it sees the same compiled `AnaalIJzerSettings` metadata value as the analyzer. It can also generate documentation directly from a specific XML settings file.

```powershell
dotnet tool install --global RonSijm.AnaalIJzer.Arse
```

Run `arse` without arguments for the interactive terminal interface built with [RazorConsole](https://github.com/RazorConsole/RazorConsole). Path fields show matching directories and relevant files while you type. Use Up/Down to select a suggestion, Right Arrow to complete it without leaving the field, or Tab to apply the selected or shared-prefix completion before moving on. Interactive architecture inspection displays its report before writing anything; choose `Save` afterward to select the output file. Supply a command to use the same executable headlessly:

```cmd
arse generate-config --project src\MyApp\MyApp.csproj --output Architecture.anl
arse generate-config --solution src\MyApp.slnx --strategy helpful --output Architecture.anl
arse generate-config --project src\MyApp\MyApp.csproj --strategy conventions --minimum-confidence 0.95 --minimum-support 10 --generate-documentation --include-input
arse export-config --project src\MyApp\MyApp.csproj --output Architecture.anl
arse documentation --project src\MyApp\MyApp.csproj --output docs\architecture-documentation.md --force
arse documentation --config Architecture.anl --output docs\architecture-documentation.md --force
arse report        --project src\MyApp\MyApp.csproj --output docs\architectural-violations.md --force
arse report        --solution src\MyApp.slnx --output docs\architectural-violations.md --force
arse inspect       --project src\MyApp\MyApp.csproj --output docs\architecture-health.md --force
arse inspect       --solution src\MyApp.slnx --output docs\architecture-health.md --force
arse merge-config  --config Shared.anl --config Project.anl --output Architecture.anl --force
arse split-config  --config Architecture.anl --output ArchitectureRules --force
```

`generate-config` inspects source-defined types and the dependency sites already present in the project. It infers layers from the first namespace segment below the project's common namespace, falling back to familiar type suffixes such as `Controller`, `Service`, `Repository`, `Handler` and `Projection`. The command writes both `Architecture.anl` and a local `AnaalIJzer.xsd`, then runs the analyzer against the generated XML before accepting the result.

The generation strategy controls how observed dependencies become rules:

| Strategy | Behavior |
|---|---|
| `snapshot` | The default. Every observed layer edge and dependency site becomes an `AllowedDependency`, producing a passing description of the current structure. |
| `helpful` | A gentle baseline. For projects it behaves like a current-structure snapshot with softer wording. For solutions it creates one layer per C# project assembly using `<Assembly exactName="...">` and permits observed project-to-project dependency sites. |
| `conventions` | Infers dominant edges and writes minority caller types into `<Exceptions>`, producing a passing ratchet that blocks new callers from following those outliers. |

Convention inference is configurable:

| Option | Default | Meaning |
|---|---:|---|
| `--minimum-confidence` | `0.90` | Minimum share of active caller types in a layer that must use an edge. The generator counts distinct caller types, not raw syntax occurrences. |
| `--minimum-support` | `5` | Minimum number of distinct callers that must use an edge before it can be treated as a convention. |

Think of confidence as **“is this edge popular enough?”** and support as **“have we seen enough callers to trust that percentage?”** An edge must pass both thresholds.

#### One project, different thresholds

Suppose Arse observes ten distinct active caller types in the `Presentation` layer:

| Existing callers | Observed dependency | Confidence | Support |
|---:|---|---:|---:|
| 8 endpoints | `Presentation --> Application` | `8 / 10 = 0.80` | `8` |
| 2 endpoints | `Presentation --> Persistence` | `2 / 10 = 0.20` | `2` |

An **active caller** is a distinct type in the source layer that has at least one observed outgoing dependency. An endpoint that mentions `Application` ten times still contributes one caller, not ten. The confidence and support comparisons are inclusive: an edge with confidence `0.80` and support `8` passes thresholds of exactly `0.80` and `8`.

Run convention generation several times against that same project, changing only the thresholds:

```cmd
arse generate-config --project Shop.csproj --strategy conventions --minimum-confidence 0.75 --minimum-support 5 --output Architecture.75-5.anl
arse generate-config --project Shop.csproj --strategy conventions --minimum-confidence 0.90 --minimum-support 5 --output Architecture.90-5.anl
arse generate-config --project Shop.csproj --strategy conventions --minimum-confidence 0.75 --minimum-support 9 --output Architecture.75-9.anl
arse generate-config --project Shop.csproj --strategy conventions --minimum-confidence 0.20 --minimum-support 2 --output Architecture.20-2.anl
```

The same evidence now produces four different results:

| Confidence | Support | Edges that qualify as conventions | Generated result |
|---:|---:|---|---|
| `0.75` | `5` | `Presentation --> Application` | Writes only the Application edge. The two Persistence callers become exact-name exceptions under the generated Presentation matcher. |
| `0.90` | `5` | None: `0.80` is below `0.90` | Evidence is ambiguous, so both observed edges are preserved as a snapshot and no exceptions are added. |
| `0.75` | `9` | None: support `8` is below `9` | Evidence is ambiguous, so both observed edges are preserved as a snapshot and no exceptions are added. |
| `0.20` | `2` | Both edges | Writes both edges as conventions. There are no rejected edges, so no exceptions are needed. |

The first invocation treats `Presentation --> Application` as the dominant convention. Its generated output is conceptually:

```xml
<Layer name="Presentation">
  <Namespace regex="^Shop\.Presentation(?:\.|$)">
    <Exceptions>
      <Class exactFullName="Shop.Presentation.LegacyAdminEndpoint" />
      <Class exactFullName="Shop.Presentation.ImportEndpoint" />
    </Exceptions>
  </Namespace>
</Layer>

<AllowedDependency from="Presentation" to="Application" />
```

The two outlier endpoint names are illustrative; Arse writes the actual fully qualified caller names it found. If **no** edge from a source layer reaches both thresholds, Arse does not guess: it preserves every observed edge from that layer as an ambiguous snapshot.

The executable counterpart is [`ConfigurationGenerator_AppliesDifferentThresholdsToTheSameEvidence`](../../src/Tests/RonSijm.AnaalIJzer.Tooling.Tests/Tooling/ToolingTests.cs#L89). Its four theory cases run this same 8/2 setup with the four threshold combinations above and verify the generated edges, ambiguity fallback, and exceptions.

Generated `<Exceptions>` use the analyzer's existing ratchet semantics: the caller is exempt from that layer matcher, so all of that caller's dependencies are grandfathered. Review these entries before adopting the file. Convention mode identifies statistically dominant structure; it cannot prove architectural intent.

Add `--generate-documentation` to write `architecture-documentation.md` beside the generated XML. The document includes the evidence counts behind inferred edges, the project types resolved by each matcher, concrete code usages permitted by each allowed dependency, generated exceptions as unclassified types, and any current analyzer violations. Add `--include-input` when the document should also contain a fenced copy of the generated XML.

`export-config` writes the evaluated inline XML, so `typeName="{nameof(OrderRepository)}"` becomes `typeName="OrderRepository"` in the persisted file. `documentation` accepts either a project for compiled inline settings and project-backed XML or a specific XML file directly. `report` accepts a project or solution; solution mode opens every C# project in the solution, runs the same analyzer pass per project, and aggregates the diagnostics into one Markdown report. `documentation` and `report` use `documentationPath` / `reportPath` from the config when the output is omitted. Solution `report` uses the first configured project as the representative settings source; if no `reportPath` is enabled there, it defaults to `architectural-violations.md` beside the solution.

`inspect` (aliases: `validate`, `doctor`, `health`) accepts a project, solution, or XML file and writes `architecture-health.md`. XML inspection reports malformed settings, missing includes, invalid matchers, unknown layer references, and configured cycles. Project inspection additionally reports unclassified or ambiguously classified types, unmatched matchers, stale exceptions, unused allowed edges, observed dependency cycles, and current analyzer violations. Solution inspection runs that same project inspection for every C# project and aggregates the findings into one report. Headless Arse exits with code `3` when findings require review.

`merge-config` recursively replaces `<Include>` elements with their referenced rules and writes one self-contained XML file. Repeated references resolving to the same path are included once. Root settings such as `requireRecognizedDependencies`, report paths, documentation paths and the XSD location are preserved and rebased relative to the merged output.

`split-config` treats `AllowedDependency` and `BlockedDependency` entries as an undirected graph for grouping purposes. When the configuration contains disconnected graphs, it writes:

- `Architecture.anl` as the new manifest.
- One `Graph.XX.<layers>.anl` file per disconnected dependency graph.
- `Shared.anl` for global rules such as `<Forbidden>`, when needed.

The manifest includes every generated file, so it remains a complete replacement for the original configuration. Wildcard dependencies connect every named layer and therefore prevent those layers from being split into separate graphs. In Arse's interactive mode, enter multiple merge inputs separated by semicolons.

Arse's interactive and headless modes share `RonSijm.AnaalIJzer.Tooling`. Its `ToolOperationCatalog`, `ToolRequest` and `ToolRunner` own the available operations, supported inputs, validation and execution behavior, keeping both modes in feature parity.
