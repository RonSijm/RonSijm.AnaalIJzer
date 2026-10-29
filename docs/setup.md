## Setup

### 1. Reference the analyzer

Add the analyzer package to the project you want to validate:

```powershell
dotnet add package RonSijm.IJzer
```

Or add the package reference directly to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="RonSijm.IJzer" Version="0.3.5" PrivateAssets="all" />
</ItemGroup>
```

`PrivateAssets="all"` keeps the analyzer as a development-time dependency and prevents it from flowing transitively to projects that reference yours. Leave it out and everyone downstream inherits your layering opinions, which is a conversation best not started inside somebody else's build log.

### 2. Create the configuration file

Add a file called `Architecture.anl` to the **root of the project you want to analyze**:

```xml
<ArchitecturalLevels>

  <Layer name="Presentation">
    <Class endsWith="Endpoint" />
  </Layer>

  <Layer name="Application">
    <Class endsWith="Service" />
    <Class endsWith="Manager" />
    <Class endsWith="Coordinator" />
  </Layer>

  <Layer name="Persistence">
    <Class endsWith="Repository" />
  </Layer>

  <AllowedDependency from="Presentation" to="Application" />
  <AllowedDependency from="Application" to="Persistence" />

</ArchitecturalLevels>
```

### Why `.anl` instead of `.xml`?

`Architecture.anl` is an XML document. I originally used the ordinary `.xml` extension and later changed only the extension, not the format. Settings still use the `<ArchitecturalLevels>` root, standard XML tooling, and the IJzer XSD schema.

I chose `.anl` for a couple of practical reasons:

- It gives the settings file an architectural identity instead of making it look like unrelated application data.
  - A generic `Architecture.xml` tends to get filed under "legacy config of uncertain ownership" and removed during a tidy-up sprint.
- It gives the tools a stable file type to recognize.
  - IJzer Forge and the standalone graph editor can register themselves as `.anl` handlers.
  - The Visual Studio companion can open an `.anl` file in its dependency-graph editor.

Add an XSD schema hint when you want XML-aware editors to validate element and attribute names while you edit. The schema is [IJzer.xsd](../src/Main/RonSijm.IJzer/Scheme/IJzer.xsd); generated configurations can place a copy beside `Architecture.anl`:

```xml
<ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                     xsi:noNamespaceSchemaLocation="IJzer.xsd">
  <!-- Rules go here. -->
</ArchitecturalLevels>
```

### 3. Register the file as an AdditionalFile

Tell MSBuild to pass the file to Roslyn:

```xml
<ItemGroup>
    <AdditionalFiles Include="Architecture.anl" />
</ItemGroup>
```

If the config uses `<Include>`, register the included settings files too:

```xml
<ItemGroup>
    <AdditionalFiles Include="*.anl" />
</ItemGroup>
```

The analyzer uses `Architecture.anl` as the explicit top-level settings file convention; other settings files are only read when referenced through `<Include>` or passed directly to IJzer Forge.

Skipping this step is the single most common setup mistake. Roslyn never receives the file, the analyzer finds no configuration, and it reports nothing - which looks exactly like a perfectly layered codebase until someone checks.

### The Error List shows an IJzer error, but the editor shows no squiggle

If the Error List contains an `ARCH_*` diagnostic with the correct file and line, the analyzer is loaded and has produced a source diagnostic. A missing underline then belongs to the editor's live-analysis or highlighting settings, not to `Architecture.anl`.

Check these settings:

1. In Visual Studio, open `Tools > Options > Text Editor > C# > Advanced` and make sure `Run background code analysis for` is not set to `None`. `Current document` is enough for the file you are editing.
2. If ReSharper is installed, open `ReSharper > Options > Environment > Editor > Visual Studio Features` and clear both squiggle-suppression options while testing:
   - `Hide all Visual Studio squiggles and file health indicator`;
   - `Hide only those Visual Studio squiggles that duplicate ReSharper code analysis highlightings`.
   - JetBrains documents that the first option hides Visual Studio's error highlighting and leaves only ReSharper highlighting visible. The duplicate-only option has also hidden non-duplicate custom Roslyn diagnostics in affected ReSharper versions.
   - IJzer is a Roslyn analyzer, so its native squiggle is one of the Visual Studio highlights hidden by that option.
3. After replacing a local analyzer package, close and reopen the solution. Visual Studio keeps analyzer assemblies loaded for the lifetime of the project session and may otherwise continue using the previous DLL.

The relevant behavior is documented by [Visual Studio live code analysis](https://learn.microsoft.com/en-us/visualstudio/code-quality/configure-live-code-analysis-scope-managed-code?view=visualstudio) and [ReSharper's Visual Studio Features options](https://www.jetbrains.com/help/resharper/Reference__Options__Environment__Visual_Studio_Integration.html).

### 4. Share the same config with `Directory.Build.props`

If several projects should use the same `Architecture.anl`, put the XML next to a solution-level `Directory.Build.props` and register it there instead of copying the file into every project:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="RonSijm.IJzer" Version="0.3.5" PrivateAssets="all" />
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)Architecture.anl" Link="Architecture.anl" />
  </ItemGroup>
</Project>
```

`Directory.Build.props` is imported by every project below its folder. `$(MSBuildThisFileDirectory)` keeps the path anchored to the props file, so every project receives the same config file regardless of where its `.csproj` lives. Per-project copies drift, and the copy that ends up being authoritative is never the one you edited.

If the analyzer is already referenced somewhere else, keep that reference and centralize only the config file:

```xml
<Project>
  <ItemGroup>
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)Architecture.anl" Link="Architecture.anl" />
  </ItemGroup>
</Project>
```

### 5. Optional: inline settings with `AssemblyMetadata`

For small examples and throwaway projects, you can put the XML directly in code with the built-in `AssemblyMetadataAttribute`. Because the value is C#, exact type matches can use `nameof(...)` instead of fragile string literals:

```csharp
using System.Reflection;

[assembly: AssemblyMetadata("IJzerSettings", $"""
<ArchitecturalLevels>
  <Layer name="Presentation">
    <Class endsWith="Endpoint" />
  </Layer>

  <Layer name="Application">
    <Class endsWith="Service" />
  </Layer>

  <Layer name="Persistence">
    <Class typeName="{nameof(OrderRepository)}" />
  </Layer>

  <AllowedDependency from="Presentation" to="Application" />
  <AllowedDependency from="Application" to="Persistence" />
</ArchitecturalLevels>
""")]

public sealed class OrderRepository { }
```

The analyzer recognizes `AssemblyMetadata("IJzerSettings", "...")` and reads the second constructor argument as XML. No custom helper attribute or extra package reference is needed.

If both config sources exist, `Architecture.anl` wins and the inline metadata value is ignored without comment. If carefully crafted inline rules suddenly stop applying, look for a file someone added last week.

The practical split is:

- **One-file examples and very small projects** use `AssemblyMetadata("IJzerSettings", ...)`.
  - Exact type-name rules can use `nameof(...)`, so a refactor breaks at compile time instead of quietly breaking the config.
- **Broader examples and real rule sets** use `.anl` files.
  - XML is easier to read once the settings need includes, nested layers, or several policy families.
- **Code fixes** can edit either source.
  - When a rule comes from `<Include>`, the fixer targets the included file that actually owns it.

See [IDE code fixes](../docs/configuration/ide-code-fixes.md) for the supported fixer matrix.

**Example project:** [`Example.InlineXml`](../Examples/Features/Example.InlineXml)

That's it. The analyzer now runs for every `.cs` file in the project.

Examples use one vocabulary at a time. Explanatory diagnostics use the restaurant roles `Customer`, `Waiter`, `Chef`, and `Pantry`. Setup and reference examples use the technical layers `Presentation`, `Application`, and `Persistence`. A diagram, code block, or explanation never maps one vocabulary onto the other, because a `Waiter` in the `Persistence` layer helps nobody.

```mermaid
flowchart LR
    Customer --> Waiter --> Chef --> Pantry
```

The self-contained projects under [`Examples/`](../Examples/) are referenced inline where their feature is documented. Most intentionally fail with documented `ARCH_<CONCERN>_<REASON>` errors; a few demonstrate clean wildcard config or generated report/documentation output. Scenario examples, such as [`Example.RepositoryQuerySurface`](../Examples/Scenarios/Example.RepositoryQuerySurface), show larger usage patterns rather than a single analyzer feature.

---
