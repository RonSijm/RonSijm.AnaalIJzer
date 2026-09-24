## Architecture documentation

For configurations that grow large, a single graph is not always enough. A diagram can show the arrows while still leaving the reader to guess what a wildcard, site filter, include, or type policy was meant to protect.

Arse can therefore generate one Markdown document containing:

- [Mermaid](https://mermaid.js.org/) dependency diagrams;
- layer and edge descriptions;
- scoped allow/block type-policy summaries;
- rules in the same order as the XML.

Set `enableDocumentation="true"` and optionally `documentationPath`, or pass `--output` directly:

```xml
<ArchitecturalLevels enableDocumentation="true"
                     documentationPath="../../docs/architecture-documentation.md"
                     description="Order-processing boundaries and query-surface rules.">
  …
</ArchitecturalLevels>
```

### What the document does with the graph

The output is one Markdown file:

- Unrelated dependency chains get separate sections and Mermaid diagrams.
  - An ordering chain and a billing chain do not need to share one confusing canvas merely because they share one settings file.
- Wildcard rules are shown after the connected graphs.
- Nested layers become Mermaid subgraphs.
- Accompanying tables use canonical layer paths.

### Choose how much source evidence to include

XML-only documentation remains the lightweight default and does not load or compile an application:

```cmd
arse documentation --config Architecture.anl --include-input
```

For a project-backed document, add `--include-code-evidence`:

```cmd
arse documentation --project MyApplication.csproj --include-code-evidence --include-input
```

The optional code-evidence section evaluates the rules against the current Roslyn compilation. It adds:

- project types resolved through each top-level `<Class>` and `<Namespace>` matcher;
- concrete caller/dependency/site usages permitted by every `<AllowedDependency>`;
- types that remain unclassified;
- current analyzer violations with diagnostic ID, site, caller, dependency, and source location.

This uses the analyzer's actual rule resolution, including document order, semantic matchers, and nested exceptions. It does not rebuild a cheaper approximation for the documentation and hope nobody notices the difference.

`--include-input` is independent of code evidence. It appends an **Input Configuration** section containing the root XML and a short note identifying it as the source for the document. With project input, `Architecture.anl` is included when present; otherwise the evaluated `AssemblyMetadata("AnaalIJzerSettings", ...)` XML is included. Without this flag, documentation output remains unchanged.

Edges with `allowedSites`, `blockedSites`, or `appliesToDescendants` get both a Mermaid label and a table row. The table also identifies the boundary gate that owns the rule. This keeps nested egress, ingress, and cascading rules distinguishable even when they resolve to the same canonical endpoints.

Descriptions are especially useful for repository query surfaces. You might allow a repository to return a transient `OrderQuery` so callers can immediately project it:

```xml
<AllowedDependency from="Persistence" to="QuerySurface"
                   allowedSites="MethodReturn, New"
                   description="Repositories may create and return query surfaces as fluent access points." />
<AllowedDependency from="QuerySurface" to="Projection"
                   allowedSites="MethodReturn, New"
                   description="Query surfaces may create projections and return only those projected objects." />
```

That documents the intent clearly: the repository owns the query surface, while outside layers receive a projected DTO rather than keeping a queryable object around where extra application logic can creep in. A diagram on its own shows which arrows exist; only the descriptions record why anyone drew them.

The documentation is written by `RonSijm.AnaalIJzer.Reporting.ArchitectureDocumentationGenerator`. Arse's `report` and `documentation` commands are independent - run either, both, or neither.

### Example documentation

This repository ships a [rendered documentation example](../Examples/Documentation/Generated/architecture-documentation.md). It is generated from [`Examples/Documentation/Example.DocumentationDemo`](../Examples/Documentation/Example.DocumentationDemo), which contains a deliberately busy settings file with descriptions on each rule node.

Regenerate it from the repo root:

```cmd
Examples\Documentation\Example.DocumentationDemo\GenerateDocumentation.bat
```

The [example batch file](../Examples/Documentation/Example.DocumentationDemo/GenerateDocumentation.bat) invokes Arse with `--config` and targets that example's `Architecture.anl` directly.

In your own project:

- use `arse documentation --project path\to\Project.csproj --include-code-evidence --include-input` when the document should include compiled code evidence;
- use `arse documentation --config path\to\Architecture.anl --include-input` when the settings alone are enough;
- pass `--output` to override `documentationPath`;
- pass `--force` to overwrite an existing file.

Documentation coverage is guarded by [`ToolRunner_GeneratesDocumentationForSupportedConfigurationFeatures`](../src/Tests/RonSijm.AnaalIJzer.IntegrationTests/ExampleApplicationIntegrationTests.cs), which runs the real `arse documentation --config` path against a feature-matrix XML containing nested layers, descriptions, type policies, exceptions, rename fixes, site filters, wildcard rules and input inclusion.

Root-level source-metadata policies such as `<AssemblyAttributePolicy>` are rendered in authored configuration order and in their own table. They describe emitted assembly attributes rather than dependency graph edges, so they appear as policy documentation instead of Mermaid nodes.

---
