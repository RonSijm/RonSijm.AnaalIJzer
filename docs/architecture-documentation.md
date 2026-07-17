## Architecture documentation

For configurations that grow large - many layers, wildcard edges, site filters, includes and type policies - a single graph is not always enough. Arse can render Markdown documentation that combines [Mermaid](https://mermaid.js.org/) dependency diagrams with layer descriptions, edge descriptions, scoped allow/block type-policy summaries and the rules in the same order as the XML. Enable a default path by setting `enableDocumentation="true"` on the `<ArchitecturalLevels>` root and optionally `documentationPath`, or pass `--output` directly:

```xml
<ArchitecturalLevels enableDocumentation="true"
                     documentationPath="../../docs/architecture-documentation.md"
                     description="Order-processing boundaries and query-surface rules.">
  …
</ArchitecturalLevels>
```

The output is a single Markdown file. If the dependency graph contains unrelated chains, each connected chain gets its own section and Mermaid diagram before wildcard rules are shown. Nested layers are rendered as Mermaid subgraphs with canonical paths in the accompanying tables. For example, an order-processing chain and a billing chain are documented separately instead of being forced into one confusing graph.

XML-only documentation remains the lightweight default and does not load or compile an application:

```cmd
arse documentation --config Architecture.anl --include-input
```

For a project-backed document, add `--include-code-evidence`:

```cmd
arse documentation --project MyApplication.csproj --include-code-evidence --include-input
```

The optional code-evidence section evaluates the rules against the current Roslyn compilation. It lists the effective project types resolved through each top-level `<Class>` and `<Namespace>` matcher, concrete caller/dependency/site usages permitted by every `<AllowedDependency>`, types that remain unclassified, and current analyzer violations with diagnostic ID, dependency site, caller, dependency and source location. Matching is attributed through the analyzer's actual rule resolution, so document order, semantic matchers and nested exceptions are respected.

`--include-input` is independent of code evidence. It appends an **Input Configuration** section containing the root XML and a short note identifying it as the source for the document. With project input, `Architecture.anl` is included when present; otherwise the evaluated `AssemblyMetadata("AnaalIJzerSettings", ...)` XML is included. Without this flag, documentation output remains unchanged.

Edges with `allowedSites`, `blockedSites`, or `appliesToDescendants` are rendered with Mermaid edge labels and a table row. The table identifies the boundary gate that owns each rule, so nested egress, ingress, and cascading rules remain distinguishable even when they resolve to the same canonical endpoints. That makes allow lists, block lists, and descendant-cascading rules visible in both the picture and the text.

Descriptions are especially useful for repository query surfaces. You might allow a repository to return a transient `OrderQuery` so callers can immediately project it:

```xml
<AllowedDependency from="Persistence" to="QuerySurface"
                   allowedSites="MethodReturn, New"
                   description="Repositories may create and return query surfaces as fluent access points." />
<AllowedDependency from="QuerySurface" to="Projection"
                   allowedSites="MethodReturn, New"
                   description="Query surfaces may create projections and return only those projected objects." />
```

That documents the intent clearly: the repository owns the query surface, while outside layers should receive a projected DTO rather than keeping a queryable object around where extra application logic can creep in.

The documentation is written by `RonSijm.AnaalIJzer.Reporting.ArchitectureDocumentationGenerator`. Arse's `report` and `documentation` commands are independent - run either, both, or neither.

### Example documentation

This repository ships a [rendered documentation example](../Examples/Documentation/Generated/architecture-documentation.md) generated from [`Examples/Documentation/Example.DocumentationDemo`](../Examples/Documentation/Example.DocumentationDemo), which contains a deliberately busy XML settings file with descriptions on each rule node. To regenerate it from the repo root:

```cmd
Examples\Documentation\Example.DocumentationDemo\GenerateDocumentation.bat
```

The [example batch file](../Examples/Documentation/Example.DocumentationDemo/GenerateDocumentation.bat) invokes Arse with `--config` and targets that example's `Architecture.anl` directly. **In your own project**, install the tool and run either `arse documentation --project path\to\Project.csproj --include-code-evidence --include-input` or `arse documentation --config path\to\Architecture.anl --include-input`. Pass `--output` to override `documentationPath`, and `--force` to overwrite an existing file.

Documentation coverage is guarded by [`ToolRunner_GeneratesDocumentationForSupportedConfigurationFeatures`](../src/Tests/RonSijm.AnaalIJzer.IntegrationTests/ExampleToolingIntegrationTests.cs), which runs the real `arse documentation --config` path against a feature-matrix XML containing nested layers, descriptions, type policies, exceptions, rename fixes, site filters, wildcard rules and input inclusion.

---
