## Visual Studio 2026 companion extension

The Visual Studio add-on is a VSIX companion for the analyzer. The analyzer remains the authority for `ARCH00X` diagnostics; the extension makes the configured architecture visible while you read and edit code.

It adds four visual workflows to Visual Studio 2026:

- **Layer information**: badges, CodeLens-style summaries, gutter glyphs, highlights, and QuickInfo explain configured layers.
- **Sites Diagnostics**: optional inline labels identify architectural sites such as constructors, fields, locals, inheritance, and generic arguments.
- **Dependency graphs**: a dockable sidebar shows the configured layer graph, follows the active file, and can focus the graph that affects it.
- **Configuration fixes**: the graph can preview and apply the same configuration-fix proposals that appear through analyzer light bulbs and Arse.

The screenshots below isolate a setting or tightly related set of settings so a reader can tell exactly what each control changes.

### Install

Build the VSIX from the repository root:

```cmd
build\Scripts\Addon\build-vs-extension.cmd
```

The script writes `RonSijm.AnaalIJzer.VisualStudio.vsix` to `build\Artifacts\VisualStudio`. Install that VSIX into Visual Studio 2026 to enable the editor companion. Each VSIX build stamps a fresh timestamp-based extension version, so Visual Studio can install a newly built local VSIX over the previous one.

The GitHub `build-vsix.yml` workflow builds and uploads the VSIX artifact on Windows. On pushes to `main`, it also submits the VSIX to Visual Studio Marketplace when the repository secret `VS_MARKETPLACE_TOKEN` is configured. Marketplace metadata lives in `src\Extensions\RonSijm.AnaalIJzer.VisualStudio\marketplace-publish.json`.

The extension reads the same `Architecture.anl` or `AssemblyMetadata("AnaalIJzerSettings", ...)` configuration as the analyzer through Visual Studio's Roslyn workspace. If no AnaalIJzer config exists, it renders nothing. If the config is invalid, the extension stays quiet and leaves the existing `ARCH006` analyzer diagnostic as the source of truth.

### Layer information on declarations

Layer indicators are controlled from Visual Studio 2026 Settings under `AnaalIJzer > Editor`:

| Option | Default | Meaning |
|---|---:|---|
| Show layer badges | On | Shows the resolved canonical layer path after a type declaration identifier. |
| Show layer metadata above declarations | On | Shows a clickable CodeLens-style AnaalIJzer summary above a type declaration. |
| Show layer badges when not in layer | Off | Shows a neutral `not in layer` badge for a type that does not match any configured layer. |
| Show global layer rules in badge hover | Off | Includes wildcard rules such as `* (any layer)` in badge hover details. |
| Show mini call graph in badge hover | On | Shows a compact one-to-one dependency chain in badge hover details when the graph is linear. |
| Gutter glyphs | On | Shows a small layer marker beside a layered type declaration. |
| Highlight code in layer | On | Shows a region-like block highlight around a layered type declaration. |
| Tint layer declaration text | Off | Applies the older line-background tint to a layered type declaration. |

Start in this settings page when you want to decide how much architectural context belongs in the editor. The controls separate fast scanning aids, such as glyphs and badges, from richer information that only appears when you hover or open CodeLens.

![AnaalIJzer editor settings](../../Examples/Assets/VisualStudio/editor-settings.png)

**Layer badge.** A badge gives a type its configured architectural role directly beside its declaration. It is the quickest way to answer “where does this type belong?” without leaving the file.

![Layer badge](../../Examples/Assets/VisualStudio/layer-badge.png)

**Layer metadata above a declaration.** The CodeLens-style summary exposes the layer's immediate relationship to the rest of the graph before you open a hover card. It is useful when reading an unfamiliar file top to bottom.

![Layer metadata above a declaration](../../Examples/Assets/VisualStudio/layer-codelens.png)

**Not in layer.** This neutral badge is deliberately opt-in: it helps distinguish a type that has not been classified from a type that simply has no dependency violation.

![Not in layer badge](../../Examples/Assets/VisualStudio/not-in-layer-badge.png)

**Gutter glyph.** The glyph keeps layer information visible while the declaration itself is off to the side or collapsed, making the editor margin useful for quick file-level scanning.

![Layer gutter glyph](../../Examples/Assets/VisualStudio/layer-gutter-glyph.png)

**Block highlight.** Highlighting frames the complete declaration rather than only tinting a line. That makes the boundary of the type easy to follow in a dense file.

![Layer block highlight](../../Examples/Assets/VisualStudio/layer-block-highlight.png)

Hovering a layered type or dependency site also shows native Visual Studio QuickInfo. Layer QuickInfo shows the canonical path, ancestry, palette slot, description when configured, which layers may call the current layer, and which layers the current layer may call.

The hover complements the lightweight badge: it answers the next architectural question, “what is this layer connected to?”, including a compact call chain when the relationship is linear.

![Layer CodeLens and QuickInfo](../../Examples/Assets/VisualStudio/layer-badge-hover-info.png)

### Layer information and Sites Diagnostics at dependency sites

Layer information and Sites Diagnostics use the same supported dependency sites but answer different questions. A layer-information label says which configured layer the referenced type belongs to. A Sites Diagnostics label says where the dependency appears in C#.

`Show all layer information` enables every layer-information label. `Show all site diagnostics` enables every site label. The controls below can also be enabled independently:

| Site | Layer-information control | Sites Diagnostics control |
|---|---|---|
| Constructor | Show Constructor Layer Information | Show Constructor Site Diagnostics |
| Method | Show Method Layer Information | Show Method Site Diagnostics |
| Method return | Show MethodReturn Layer Information | Show MethodReturn Site Diagnostics |
| Field | Show Field Layer Information | Show Field Site Diagnostics |
| Property | Show Property Layer Information | Show Property Site Diagnostics |
| Local | Show Local Layer Information | Show Local Site Diagnostics |
| Object creation | Show New Layer Information | Show New Site Diagnostics |
| Generic invocation | Show GenericInvocation Layer Information | Show GenericInvocation Site Diagnostics |
| Generic argument | Show GenericArgument Layer Information | Show GenericArgument Site Diagnostics |
| Base class | Show Inheritance Layer Information | Show Inheritance Site Diagnostics |
| Implemented interface | Show InterfaceImplementation Layer Information | Show InterfaceImplementation Site Diagnostics |
| Attribute | Show Attribute Layer Information | Show Attribute Site Diagnostics |
| Static member access | Show StaticMember Layer Information | Show StaticMember Site Diagnostics |

The labels do not create or suppress diagnostics. They make the syntactic location and resolved layer visible while the analyzer remains responsible for compile/build errors. Separate allowed, warning, unclassified, and error colors make an allowed constructor dependency distinct from a site-filtered or blocked one.

For a clean demonstration of every site in one editor tab, open [`Example.VisualStudioSiteDiagnostics`](../../Examples/Documentation/Example.VisualStudioSiteDiagnostics). It deliberately has no analyzer violations, so the layer and site labels remain easy to inspect.

**A focused site explanation.** The constructor is the smallest useful example. Its label identifies where the dependency is being introduced, while the analyzer's red squiggle remains responsible for saying whether that use is legal.

![Constructor Site Diagnostics](../../Examples/Assets/VisualStudio/site-diagnostics-constructor.png)

**A whole-file view.** The all-sites showcase makes it easier to see the difference between a type's layer and the code location that references it. Open the example, enable the relevant group of controls, and use the labelled lines to learn each site shape in context.

![All Layer Information sites](../../Examples/Assets/VisualStudio/site-layer-information-all-sites.png)

### Dependency graphs

Use `Extensions > IJzer > Show Dependency Graphs` or command search to open a dockable dependency-graph sidebar. The sidebar groups concrete layer rules into connected graphs and shows wildcard/global rules separately. The graph is the same reusable WPF editor hosted by the standalone graph editor. It supports layer grouping, user-controlled layout, connector-based dependency creation, right-click editing, nested-boundary visualization, and PNG export.

**Start with the configured structure.** With code evidence off, the graph stays focused on the intended architecture: the named layers and the allowed paths between them. This is the clearest mode for discussing or editing the rules themselves.

![Dependency graph without code evidence](../../Examples/Assets/VisualStudio/graph-no-code.png)

| Option | Default | Meaning |
|---|---:|---|
| Graph focus mode | Highlight current | Chooses `Show all graphs`, `Highlight current graph`, or `Filter to current graph` for the active editor. |
| Open .anl files in diagram editor | On | Opens or selects an `.anl` settings file in the graph editor automatically. |
| Include code evidence | Off | Includes matching project types and observed violations in graph snapshots. |

**Add evidence when investigating a real project.** Enabling code evidence adds matching-type counts and observed violations to the same graph. The dashed red connection in this capture turns an abstract rule into a concrete place to investigate.

![Dependency graph with code evidence](../../Examples/Assets/VisualStudio/graph-with-code.png)

### Configuration fixes from the graph

When the graph comes from an active C# document in a loaded Visual Studio project, it exposes `Configuration fixes` in both the root inspector and the selected layer or connection inspector. The same shared configuration-fix proposal catalog is used by Roslyn light bulbs, `arse fixes`, and the standalone graph editor. It can:

- scan the active project for fixable AnaalIJzer diagnostics;
- show the target file, risk level, and preview diff for each proposal;
- apply one selected proposal and immediately refresh the graph.

You can also right-click a layer or dependency connection and jump straight to the scoped fixer view for that selection. The proposal list is filtered to the selected layer or dependency pair, so you do not have to scan every fix in the active project by hand.

Detached `.anl` files still open in the graph editor, but they do not automatically have enough Roslyn project context to offer analyzer-backed configuration fixes.

### Status and troubleshooting

Use `Extensions > IJzer > Show Status` if the editor appears quiet. It analyzes the active document and reports whether the file is part of Visual Studio's Roslyn workspace, whether settings were found, how many layer/site indicators were produced, and whether configuration issues are suppressing visual adornments.

The companion writes diagnostic logs to Visual Studio's Activity Log and to an Output window pane named `AnaalIJzer`. If settings, menu commands, or editor visuals do not appear, start Visual Studio with logging enabled, reproduce the issue, and search the Activity Log for `AnaalIJzer`. If there are no `AnaalIJzer` entries at all, the VSIX package is not loading; if package initialization is present but no tagger entries appear, the editor MEF component is not being created for the active C# view.

For local validation, use the [Visual Studio companion manual acceptance checklist](../../docs/visual-studio-companion-manual-acceptance.md). If no adornments appear, run `Extensions > IJzer > Show Status` first. The extension reads analyzer `AdditionalFiles`, inline `AssemblyMetadata("AnaalIJzerSettings", ...)`, and as an editor-only convenience the nearest `Architecture.anl` above the active document; if the config is invalid, the companion intentionally renders nothing and leaves the `ARCH006` diagnostic as the source of truth.

### Technical notes

The VSIX uses classic Visual Studio editor extension points: MEF taggers, glyphs, inline adornments, option pages and Fonts & Colors format definitions. The shared snapshot logic lives in the analyzer assembly under `RonSijm.AnaalIJzer.Editor`, so the extension does not duplicate config parsing or layer matching.
