### `enableDocumentation` / `documentationPath` attributes

When `enableDocumentation="true"` is set, Arse uses `documentationPath` as the default output for `arse documentation`. The generated Markdown contains Mermaid dependency diagrams, site-filter labels, allowed and forbidden type-policy summaries with their scopes, and the XML rules with their descriptions in configuration order. Path resolution mirrors `reportPath`; the default is `architecture-documentation.md` next to the project. Generated documentation is only accurate while it is still being generated, which is exactly why the path belongs in configuration rather than in someone's shell history.

```xml
<ArchitecturalLevels enableDocumentation="true"
                     documentationPath="../../docs/architecture-documentation.md"
                     description="Order-processing boundaries and query-surface rules.">
  …
</ArchitecturalLevels>
```
