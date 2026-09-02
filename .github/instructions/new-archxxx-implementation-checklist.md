# New ARCHxxx Implementation Checklist

Last updated: 2026-09-01

Use this checklist whenever adding a diagnostic, changing a diagnostic's semantics, or introducing a configuration feature whose primary purpose is to produce a new `ARCHxxx` finding.

This is intentionally broader than the analyzer itself. An AnaalIJzer diagnostic is a product feature with a rule, configuration, examples, documentation, tests, editor information, and sometimes a user-selected fix.

Not every section applies to every diagnostic. Mark an item `N/A` only with a short reason in the implementation plan or pull request. Do not silently omit an entire product surface.

## 1. Define The Rule Before Coding

- [ ] Reserve the next unused `ARCHxxx` ID. Do not renumber existing diagnostics.
- [ ] Give it a concise, user-facing name that describes the architectural failure, not the implementation mechanism.
- [ ] Write the intended diagnostic message before implementing detection.
- [ ] State the exact condition that produces the finding.
- [ ] State the exact condition that does not produce the finding.
- [ ] Decide whether the diagnostic is an error, warning, or informational result, and why.
- [ ] Decide whether it is a declaration rule, dependency rule, configuration rule, project rule, observation rule, or another clearly named family.
- [ ] Identify the primary source location users should see in an IDE.
- [ ] Identify any useful additional locations.
- [ ] Define stable diagnostic properties needed by tools, reports, or fixers. Use semantic names such as `CallerLayer`, `RuleXmlPath`, `Site`, or `ViolationReason`; do not require tools to parse the message text.
- [ ] Decide which declaration targets and dependency sites are in scope. Explicitly consider `Constructor`, `Method`, `MethodReturn`, `Field`, `Property`, `Local`, `New`, `GenericInvocation`, `GenericArgument`, `Inheritance`, `InterfaceImplementation`, `Attribute`, and `StaticMember` where relevant.
- [ ] Decide how partial types, generated code, generic types, nested types, records, interfaces, delegates, and nullable/framework types behave where relevant.
- [ ] Write down known non-goals. A narrowly correct rule is preferable to a broad rule that surprises users.
- [ ] Check the rule against the restaurant vocabulary used in beginner-facing documentation. Keep the metaphor internally consistent, and keep technical copy-paste examples technically consistent rather than mixing both vocabularies.

## 2. Design Configuration Deliberately

- [ ] Decide whether the rule needs configuration at all. Prefer a useful default only when it is genuinely unambiguous.
- [ ] Identify the smallest configuration shape that can express the rule.
- [ ] Reuse existing matcher, layer, type-policy, site-filter, exception, and inheritance concepts before introducing a new XML concept.
- [ ] If a new XML element or attribute is necessary, define its placement, valid values, defaults, scope, inheritance/cascading behavior, and precedence.
- [ ] State whether repeated child elements are AND conditions or OR alternatives.
- [ ] State case sensitivity, whitespace handling, wildcard behavior, empty-value behavior, and invalid-value behavior.
- [ ] State how the rule interacts with nested layers, scoped policies, includes, `appliesToDescendants`, exceptions, allowed/blocked lists, and strict/recognized dependency requirements when applicable.
- [ ] Decide whether the configuration can be supplied through both `Architecture.anl` and inline `AssemblyMetadata("AnaalIJzerSettings", ...)`.
- [ ] If it cannot safely support inline settings, document the reason and fail closed rather than silently losing configuration.
- [ ] Define the error behavior for invalid configuration. Invalid input should produce `ARCH006` with a useful source location, not an analyzer crash or an ignored rule.
- [ ] Decide whether an included `.anl` file owns the rule and must be edited directly by a future fixer.

## 3. Update The Configuration Model And Schema

- [ ] Add or update the configuration document model in the appropriate `Core.Configuration.*` assembly.
- [ ] Keep parsing, validation, materialization, and runtime evaluation separate. Do not turn one parser method into the owner of every concern.
- [ ] Add parsing for the new element or attribute.
- [ ] Add validation for invalid values, incompatible combinations, duplicate names, invalid references, and unsupported scope.
- [ ] Add materialization into the compiled runtime configuration.
- [ ] Preserve XML source ownership, path, line, and column information for diagnostics, documentation, graph editing, and fixers.
- [ ] Update `src/Main/RonSijm.AnaalIJzer/Scheme/AnaalIJzer.xsd`, including descriptive comments and every valid enum/token value.
- [ ] Add parser and document-model tests for valid values, invalid values, defaults, and source locations.
- [ ] Add include and inline-metadata coverage if the new configuration is available in those sources.
- [ ] Confirm configuration merging, splitting, documentation generation, graph snapshots, and configuration editing preserve the new data where applicable.

## 4. Implement The Analyzer Rule

- [ ] Add the ID to `ArchitectureDiagnosticIds`.
- [ ] Add a `DiagnosticDescriptor` to `ArchitecturalDiagnostics` with title, message format, category, default severity, description, help link, and appropriate custom tags.
- [ ] Add the ID to `AnalyzerReleases.Unshipped.md`.
- [ ] Register only the necessary Roslyn analysis callbacks. Avoid registering broad syntax callbacks when symbol or operation analysis is more precise.
- [ ] Keep the analyzer entry point thin; place detection under the owning feature area in Engine/Core assemblies.
- [ ] Reuse shared matching, policy-evaluation, site-classification, source-location, and violation-record helpers rather than copying their logic.
- [ ] Respect cancellation tokens and avoid filesystem side effects during compilation.
- [ ] Avoid semantic-model work on generated code when the analyzer's generated-code policy says to ignore it.
- [ ] Ensure one source construct produces at most the intended number of diagnostics. Deduplicate partial-type, generic, and transitive findings where necessary.
- [ ] Attach every planned diagnostic property through a typed constant in `ArchitectureDiagnosticProperties`.
- [ ] Ensure the diagnostic has the correct source location and useful additional locations.
- [ ] Verify the rule works in normal compilation, design-time analysis, and `MSBuildWorkspace` analysis.

## 5. Decide Whether A Fix Is Honest And Useful

- [ ] Ask whether a deterministic source fix exists.
- [ ] Ask whether a deterministic configuration fix exists.
- [ ] Do not add a fixer that merely suppresses, weakens, or hides a problem while claiming to solve it.
- [ ] If a fix needs a user architecture decision, present explicit named choices rather than selecting one automatically.
- [ ] If no honest fix exists, document that decision in the IDE-code-fix matrix and add a no-proposal test when appropriate.

### Source Code Fixer

- [ ] Add a Roslyn source fixer only when the edited code is unambiguous and preserves intended behavior.
- [ ] Use stable diagnostic properties rather than parsing messages.
- [ ] Preserve trivia, formatting, comments, and references where Roslyn APIs support it.
- [ ] Test the fix against the exact violating syntax and against near misses where no code action should appear.
- [ ] Verify Fix All behavior is safe; return no Fix All provider when it is not.

### Configuration Fixer

- [ ] Implement the proposal through the shared Diagnostics/Application configuration-fix path, not independently in a UI host.
- [ ] Use `ConfigurationCodeFixEditor` and source discovery so `.anl`, included `.anl`, and inline metadata edit the actual owning source.
- [ ] Provide a precise title that says exactly what XML configuration change will happen.
- [ ] Classify the proposal as `Safe`, `Guided`, or `HighRisk`.
- [ ] Prefer the narrowest exact edit over a broad wildcard, exception, or policy relaxation.
- [ ] Include a previewable diff and ensure applying the proposal changes configuration only.
- [ ] Add the diagnostic to `ArchitecturalLevelCodeFixProvider` only if it can surface a valid Roslyn code action.
- [ ] Confirm the shared proposal collector exposes it to Arse and graph hosts.
- [ ] Add XML, inline metadata, and included-file ownership tests whenever the fixer can touch those source types.
- [ ] Re-analyze after the fix in an application-level test to prove it resolves the intended finding without creating unrelated changes.

## 6. Make It Visible In Tooling

- [ ] Update the shared application operation layer if the new rule affects inspect, report, documentation, configuration generation, or fixer output.
- [ ] Update Arse headless help, operation validation, TUI display, and tests when users need to invoke or understand the feature there.
- [ ] Update graph model/snapshot output if the rule changes layers, edges, type policies, evidence, violations, or editable configuration.
- [ ] Update the standalone WPF graph editor if the rule can be displayed, inspected, edited, or fixed there.
- [ ] Update the Visual Studio extension if the rule should appear in layer information, site diagnostics, QuickInfo, CodeLens, the graph tool window, or graph editing.
- [ ] Keep Visual Studio and WPF hosts as presentation layers. Shared parsing, analysis, fixes, and persistence belong in reusable assemblies.
- [ ] Add only relevant UI controls. Do not create a toggle or visual indicator merely because a diagnostic exists.
- [ ] Check light and dark Visual Studio themes for any new visual state.
- [ ] Keep graph labels concise, actionable, and compatible with nested boundary grouping.

## 7. Create Focused Examples

- [ ] Create `Examples/Diagnostics/Example.Archxxx.<ShortCaseName>/` for the diagnostic.
- [ ] Use a focused `Examples/Features/Example.<FeatureName>/` too if the configuration model deserves a separate explanation.
- [ ] Use `Examples/Scenarios/` only when multiple projects or a broader real-world pattern are required.
- [ ] Keep each example minimal: it should teach one behavior rather than become a configuration dump.
- [ ] Use inline `AssemblyMetadata` for a simple one-file example; use `Architecture.anl` for multi-file, include-based, or scenario examples.
- [ ] Put `// ReSharper disable All - Justification: Example File` at the top of every example source file.
- [ ] Use a descriptive source file name when the example has more than one behavior. Avoid generic `Example.cs` when a name such as `PropertySetterNotAllowedExample.cs` explains the case better.
- [ ] Include at least one allowed case and one intended violating case where this makes semantic sense.
- [ ] Make comments explain the architectural reason, not merely restate the diagnostic ID.
- [ ] Ensure all names, comments, diagrams, and prose use one coherent vocabulary.
- [ ] Configure XSD validation correctly for file-based `.anl` examples.
- [ ] Add the example project to the appropriate `.slnx` files and example indexes.
- [ ] Add the intended diagnostic count to `ExampleBuildExpectationCatalog`.
- [ ] Add expected configuration-fix availability to `ExampleFixExpectationCatalog` when a fixer exists.
- [ ] Add an explicit no-proposal expectation when the lack of a fixer is a deliberate product decision.
- [ ] Generate or refresh the example graph image if the example is documented with a graph.

## 8. Test The Rule At Every Relevant Level

### Core And Parser Tests

- [ ] Unit-test parsing, validation, defaulting, configuration precedence, and invalid configuration.
- [ ] Test matcher behavior and policy evaluation independently when the rule uses them.
- [ ] Test nested layers, scoped rules, includes, wildcard rules, and inline metadata when relevant.

### Analyzer Tests

- [ ] Test one minimal violation.
- [ ] Test one minimal allowed counterpart.
- [ ] Test every applicable site and declaration target.
- [ ] Test every diagnostic route or reason the rule can produce.
- [ ] Test diagnostic message, ID, location, and important properties.
- [ ] Test partial declarations, generics, inherited symbols, generated-code behavior, and duplicate suppression where relevant.
- [ ] Test invalid configuration produces `ARCH006` rather than crashing.

### Fixer Tests

- [ ] Test offered titles and risk classification.
- [ ] Test applying every supported fix option.
- [ ] Test no action is offered for ambiguous or unsupported cases.
- [ ] Test XML, inline metadata, and included-file ownership as applicable.
- [ ] Test the post-fix analyzer result.

### Application, Integration, And Host Tests

- [ ] Add shared Application/Workspace tests for project and solution inputs when the feature uses workspace analysis or config fixes.
- [ ] Add example integration coverage that compiles the real project and checks expected diagnostic IDs/counts.
- [ ] Add Arse command/TUI tests if behavior is exposed there.
- [ ] Add WPF graph-editor tests for loading, display, filtering, editing, applying fixes, and persistence as applicable.
- [ ] Add Visual Studio extension tests for snapshot/tooltip/tagger/graph state behavior as applicable.
- [ ] Run manual Visual Studio Experimental Instance checks for changes that cannot be meaningfully covered outside the IDE.

## 9. Document It Where Users Will Look

- [ ] Add a page under `docs/diagnostics/archxxx-<slug>.md`.
- [ ] Add the diagnostic to `docs/diagnostics/index.md`.
- [ ] Add the page to `docs/_readme-order.txt` when it belongs in the generated README.
- [ ] Document the rule's purpose, message, trigger conditions, non-trigger conditions, configuration syntax, defaults, and examples.
- [ ] Link the exact diagnostic and feature examples beside the explanation.
- [ ] Document all user-visible diagnostic properties that tools or integrations can rely on.
- [ ] Update the configuration-reference page if XML elements or attributes changed.
- [ ] Update the mental-model and precedence documentation if the new rule changes how existing concepts interact.
- [ ] Update documentation/report/diagram pages if generated output gains a new section or column.
- [ ] Update `docs/configuration/ide-code-fixes.md` with the available fix or the intentional absence of one.
- [ ] Update Arse, WPF, and Visual Studio component docs when their user-visible behavior changes.
- [ ] Regenerate the root README with `pwsh -NoProfile -File docs\build-readme.ps1`.
- [ ] Validate links and generated documentation with `build\Scripts\Docs\check-docs.ps1`.

## 10. Preserve Structure And Code Quality

- [ ] Put new code in the smallest appropriate assembly. Favor small specialized assemblies with real project references over source-file linking or a growing host project.
- [ ] Keep analyzer, configuration, policy evaluation, workspace, output, graph, editor, and host concerns separate.
- [ ] Use feature-based folders and matching namespaces; do not add feature files to project roots.
- [ ] Keep `GlobalUsings.cs` in `Properties/`.
- [ ] Keep public APIs deliberate and documented when consumed across assembly boundaries; default to `internal` otherwise.
- [ ] Do not introduce a pass-through interface unless it is a real seam with multiple implementations or a testing need.
- [ ] Keep methods, models, and files small enough to read. Split a file that grows beyond roughly 500 lines unless there is a compelling cohesive reason not to.
- [ ] Reuse typed models and helpers rather than string-parsing diagnostics or duplicating XML editing logic.
- [ ] Follow the repository formatting conventions: tabs/CRLF for C#, one-line method signatures, one-line simple get-only properties, and local `result` variables before simple returns.
- [ ] Avoid unrelated formatting or refactoring churn in the feature change.
- [ ] Check all changed project references; remove accidental host/UI dependencies from reusable libraries.

## 11. Final Verification And Delivery

- [ ] Build every changed project.
- [ ] Run every affected unit-test project.
- [ ] Run analyzer tests and the real-example integration tests.
- [ ] Run Application/Workspace tests for workspace-backed behavior.
- [ ] Run Arse, WPF, and Visual Studio test projects when their surfaces changed.
- [ ] Build the standalone WPF tool and VSIX when their assemblies changed.
- [ ] Build the relevant examples directly in Release and verify only intended diagnostics appear.
- [ ] Run documentation generation and checks.
- [ ] Run `git diff --check`.
- [ ] Inspect `git status --short --untracked-files=all` and verify no generated clutter, stale graph images, or temporary files are included unintentionally.
- [ ] Shut down build servers if the repository workflow requires it: `dotnet build-server shutdown`.
- [ ] Record anything intentionally not implemented, with a reason, in the diagnostic doc or feature plan.

## Suggested Completion Note

When closing the work, state:

- the new `ARCHxxx` rule and its user-visible behavior;
- configuration and precedence decisions;
- example project and expected diagnostic behavior;
- fixer availability or intentional absence;
- tooling surfaces updated;
- the exact tests/builds run;
- any consciously deferred work.
