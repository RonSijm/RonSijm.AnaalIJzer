## Visual Studio companion extension

The analyzer already reports the actual `ARCH00X` diagnostics in Visual Studio. The companion extension adds editor-only context on top of those diagnostics: it shows which configured layer a type belongs to, and it can label dependency sites while you are reading code.

Build the VSIX from the repository root:

```cmd
build\Scripts\build-vs-extension.cmd
```

The script writes `RonSijm.AnaalIJzer.VisualStudio.vsix` to `build\Artifacts\VisualStudio`. Install that VSIX into Visual Studio 2026 to enable the editor companion. Each VSIX build stamps a fresh timestamp-based extension version, so Visual Studio can install a newly built local VSIX over the previous one.

The GitHub `build-vsix.yml` workflow builds and uploads the VSIX artifact on Windows. On pushes to `main`, it also submits the VSIX to Visual Studio Marketplace when the repository secret `VS_MARKETPLACE_TOKEN` is configured. Marketplace metadata lives in `src\Extensions\RonSijm.AnaalIJzer.VisualStudio\marketplace-publish.json`.

The extension reads the same `Architecture.anl` or `AssemblyMetadata("AnaalIJzerSettings", ...)` configuration as the analyzer through Visual Studio's Roslyn workspace. If no AnaalIJzer config exists, it renders nothing. If the config is invalid, the extension stays quiet and leaves the existing `ARCH006` analyzer diagnostic as the source of truth.

Layer indicators are controlled from Visual Studio 2026 Settings under `AnaalIJzer > Editor`:

| Option | Default | Meaning |
|---|---:|---|
| Show layer badges | On | Shows the resolved canonical layer path after class, interface, struct and record declarations. |
| Show layer badges when not in layer | Off | Shows a neutral `not in layer` badge for type declarations that do not match any configured layer. |
| Gutter glyphs | On | Shows a small layer marker beside layered type declarations. |
| Highlight code in layer | On | Highlights layered type declaration blocks using fixed Fonts & Colors entries named `AnaalIJzer Layer 01` through `AnaalIJzer Layer 16`, plus a subtle block outline in the editor. Layer paths map to slots deterministically by configuration document order. |
| Individual site diagnostics | Off | Each supported site has its own switch, such as `Show Constructor Site Diagnostics`, `Show Local Site Diagnostics`, `Show InterfaceImplementation Site Diagnostics`, and `Show StaticMember Site Diagnostics`. |
| Graph focus mode | Highlight current | Controls whether the dependency graph tool window shows every graph, highlights the graph that affects the active editor, or filters to only the active graph. |

You can also toggle site labels from `Tools > AnaalIJzer: Toggle Sites Diagnostics` or command search. The command turns every site label on when none are enabled, and turns every site label off when at least one is enabled. These labels do not create or suppress diagnostics; they only make the syntax site visible while the analyzer remains responsible for compile/build errors. Site labels use separate allowed, warning, unclassified, and error colors so an allowed constructor dependency does not look the same as a site-filtered or blocked dependency.

Use `View > Other Windows > AnaalIJzer: Show Dependency Graphs` or command search to open a dockable dependency-graph sidebar. The sidebar groups concrete layer rules into connected graphs and shows wildcard/global rules separately. When the active editor contains a type assigned to a layer, the configured graph focus mode can either keep all graphs visible and highlight the affected one, or show only the affected graph.

Use `Tools > AnaalIJzer: Show Status` if the editor appears quiet. It analyzes the active document and reports whether the file is part of Visual Studio's Roslyn workspace, whether settings were found, how many layer/site indicators were produced, and whether configuration issues are suppressing visual adornments.

Hovering a layered type or dependency site shows native Visual Studio QuickInfo. Layer QuickInfo shows the canonical path, ancestry, palette slot, description when configured, which layers may call the current layer, and which layers the current layer may call. Site QuickInfo shows the site name, caller, dependency, status, diagnostic ID when present, and the same denial reason used by the analyzer snapshot.

The companion writes diagnostic logs to Visual Studio's Activity Log and to an Output window pane named `AnaalIJzer`. If settings, menu commands, or editor visuals do not appear, start Visual Studio with logging enabled, reproduce the issue, and search the Activity Log for `AnaalIJzer`. If there are no `AnaalIJzer` entries at all, the VSIX package is not loading; if package initialization is present but no tagger entries appear, the editor MEF component is not being created for the active C# view.

The VSIX uses classic Visual Studio editor extension points: MEF taggers, glyphs, inline adornments, option pages and Fonts & Colors format definitions. The shared snapshot logic lives in the analyzer assembly under `RonSijm.AnaalIJzer.Editor`, so the extension does not duplicate config parsing or layer matching.

For local validation, use the [Visual Studio companion manual acceptance checklist](../../docs/visual-studio-companion-manual-acceptance.md). If no adornments appear, run `Tools > AnaalIJzer: Show Status` first. The extension reads analyzer `AdditionalFiles`, inline `AssemblyMetadata("AnaalIJzerSettings", ...)`, and as an editor-only convenience the nearest `Architecture.anl` above the active document; if the config is invalid, the companion intentionally renders nothing and leaves the `ARCH006` diagnostic as the source of truth.
