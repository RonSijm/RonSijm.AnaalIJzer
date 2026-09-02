## Configuration fixers

Configuration fixers are the part of AnaalIJzer that edit the architecture settings instead of editing your C# code.

For a configured cycle (`ARCH007`), the fixer presents the exact allowed edges in the cycle and lets you choose one to block or remove. It does not choose an architectural direction on your behalf. An observed source-code cycle (`ARCH018`) has no configuration fixer: changing a rule would not remove the code dependency that created it.

That distinction matters:

- a **source fix** changes code, such as renaming a declaration;
- a **configuration fix** changes `Architecture.anl` or inline `AssemblyMetadata("AnaalIJzerSettings", ...)`.

Use configuration fixers when the code is acceptable but the rule set needs a narrow, explicit update.

### What they are for

The fixers are designed for maintenance work that is repetitive but still deterministic:

- add one missing `<AllowedDependency>`;
- append one missing `allowedSites` token;
- remove one blocking `blockedSites` token;
- classify one unknown dependency into an existing layer;
- add one exception entry;
- add one allow-list or boundary entry-point item.

They are intentionally conservative. They do not try to redesign the architecture for you.
When there is more than one plausible architecture edit, they should present named choices and let you pick one.

### Where they work

The same shared fixer catalog is reused by three hosts:

| Host | What you can do |
|---|---|
| Visual Studio / Rider light bulbs | apply source fixes and configuration fixes from diagnostics |
| Visual Studio dependency graph | preview and apply configuration fixes from the active project, including layer- and dependency-scoped graph selections |
| Arse | list proposals with `arse fixes` and apply one with `arse apply-fix` |
| WPF graph editor | load, preview, filter, and apply configuration fixes from project or solution input |

The host UI changes, but the proposal generation is shared. That keeps the terminal, graph editor, and IDE from disagreeing about what a safe config change looks like.

### Ownership rules

Every proposal targets the real owning source:

- if the rule came from the root `Architecture.anl`, that file is edited;
- if the rule came from an included `.anl`, that included file is edited;
- if the rule came from inline `AssemblyMetadata`, the source file that contains that assembly attribute is edited.

That way the fix does not silently flatten includes or move rules into the wrong file.

### Risk labels

Each proposal is ranked as one of these:

- `Safe`: a narrow edit with one obvious meaning;
- `Guided`: still deterministic, but it changes policy more directly;
- `High risk`: technically valid, but broad enough that you should read it carefully first.

In practice:

- adding a single missing site token is `Safe`;
- adding a new `<AllowedDependency>` is usually `Guided`;
- flipping one exact reverse `<AllowedDependency>` for `ARCH004` is `Guided`;
- widening API-surface policy is `High risk`.

### Typical flow

1. AnaalIJzer reports a diagnostic.
2. The fixer catalog checks whether a deterministic config edit exists.
3. A proposal is created with title, reason, target file, and preview diff.
4. The host shows that proposal.
5. You choose whether to apply it.
6. The project or config is analyzed again.

The important part is that the UI never edits XML text directly. It applies the same structured edit model the other hosts use.

### Selection-scoped graph fixes

The graph editors support two views of the same proposal list:

- a **root view** with every proposal found for the current project or solution;
- a **selection-scoped view** filtered to the chosen layer or dependency pair.

In the Visual Studio graph and the standalone WPF graph editor, right-clicking a layer or dependency connection and choosing `Show configuration fixes` switches directly to that filtered view.

That is useful when a large project has many proposals but you are only investigating one boundary.

### Current diagnostic coverage

The current configuration-fix coverage is documented in [IDE code fixes](ide-code-fixes.md). That page is the support matrix; this page is the mental model.

### Where to verify it

The feature is intentionally covered at several levels:

- analyzer fixer tests: `src/Tests/RonSijm.AnaalIJzer.Analyzer.Tests/Diagnostics/`
- application-level project and solution flows: `src/Tests/RonSijm.AnaalIJzer.Application.Tests/ApplicationOperations/ApplicationOperationsTests.ConfigurationFixes.cs`
- real example-project expectations: `src/Tests/RonSijm.AnaalIJzer.IntegrationTests/ExampleConfigurationFixIntegrationTests.cs`
- WPF graph-editor selection filtering: `src/Tests/RonSijm.AnaalIJzer.GraphEditor.Wpf.Tests/Controls/ArchitectureGraphEditorControlPersistenceTests.ConfigurationFixSelection.cs`
- Visual Studio graph context preservation: `src/Tests/RonSijm.AnaalIJzer.VisualStudio.Tests/Graphs/ArchitectureGraphToolWindowStateTests.cs`
- Arse command-line support: `src/Tests/RonSijm.AnaalIJzer.Arse.Tests/`

### When not to use one

Sometimes the right fix is still a code change, not a config change:

- renaming a declaration so it matches a naming rule;
- moving a type into the correct file or folder;
- extracting an interface or projection to the right layer;
- deleting a bad dependency instead of allowing it.

AnaalIJzer should help with both kinds of repair, but it should not use a config escape hatch when the code is simply wrong.
