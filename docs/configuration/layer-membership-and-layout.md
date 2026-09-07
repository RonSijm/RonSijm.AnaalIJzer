## Layer Membership and Physical Layout

Layers describe a type's architectural role. They are deliberately defined by `Architecture.anl`, using logical matchers such as `<Class>`, `<Namespace>`, and `<Assembly>`.

```xml
<Layer name="Application">
  <Class endsWith="Service" />
  <Namespace endsWith=".Application" />
</Layer>
```

That rule says what a type *is*. It does not say where the type happens to live today.

### The design boundary

AnaalIJzer intentionally does not assign a layer from:

- a `.csproj` name or project path;
- a source folder, including its child folders;
- a solution-folder entry;
- a project property such as `AnaalIJzerProjectLayer`;
- an attribute, `.editorconfig` entry, package reference, or observed call graph.

The `.anl` configuration is the single source of truth for layer definitions and membership. A type has one canonical layer path, such as `Ordering/Application`; it is not a collection of unrelated physical labels.

This keeps dependency diagnostics understandable. A `Chef` remains a `Chef` after a project rename or a source-file move. The architecture should not silently change because someone reorganized folders during spring cleaning.

### What was considered

#### Project and folder membership selectors

One possible design was to allow rules such as these:

```xml
<!-- Not supported. -->
<Layer name="Tools">
  <Project exactName="MyCompany.Tools" />
</Layer>

<Layer name="Shared">
  <Folder path="src/Shared" includeDescendants="true" />
</Layer>
```

This is convenient when a repository currently mirrors its architecture in projects or folders. It becomes misleading when one project contains contracts, application code, infrastructure, and several features, or when a folder is reorganized without intending to rewrite dependency policy. It also makes physical build layout compete with logical type matching for ownership of a layer.

#### A project-declared layer property

Another option was a project-side declaration:

```xml
<!-- Not supported. -->
<PropertyGroup>
  <AnaalIJzerProjectLayer>/Ordering</AnaalIJzerProjectLayer>
</PropertyGroup>
```

That can be attractive for reusable rule packs and `Directory.Build.props` inheritance. It was rejected because it lets a project participate in defining its own classification, splits the architecture across `.anl` and MSBuild files, and needs precedence rules when the property disagrees with the configuration. A project property is useful build metadata, but it is not the authority on whether a type is a waiter, chef, or pantry worker.

#### Solution folders and inferred membership

Solution folders are IDE organization rather than compiler input, so they are not reliable during command-line or design-time compilation. Package references, inheritance, call graphs, and method bodies are also poor membership sources: they can change as an implementation detail and would make a type's layer move unexpectedly.

### Use the focused feature instead

| Need | Use |
|---|---|
| Define a type's architectural role | `<Layer>` matchers such as `<Class>`, `<Namespace>`, and `<Assembly>` |
| Add a broader logical boundary with more specific child roles | Nested `<Layer>` elements |
| Require an already-classified type to live in a project or folder | [`<SourceLocations>`](source-locations.md) |
| Govern project-to-project references | [Project architecture](project-architecture.md) |
| Share one configuration across a directory of projects | `Directory.Build.props` plus one `Architecture.anl` |
| Add or remove a rule pack | `<Include>` and explicit `AdditionalFiles` registration |

For example, use a namespace matcher to decide that a type is part of Ordering, then use `<SourceLocations>` to require Ordering code to remain in the `Ordering/` folder. The first answers "what role does this type have?" The second answers "is that role stored in the right place?"

### Reconsidering the boundary

This is an intentional constraint, not a claim that physical structure never matters. Revisit it only when a concrete architecture cannot be expressed with logical matchers, nested layers, source-location policies, and project-architecture policies together. Any future proposal should preserve one canonical layer path, make its source visible in diagnostics and tooling, and avoid silently changing architecture when files or projects move.
