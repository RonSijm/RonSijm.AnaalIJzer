## Setup

### 1. Reference the analyzer

Add the analyzer package to the project you want to validate:

```powershell
dotnet add package RonSijm.AnaalIJzer
```

Or add the package reference directly to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="RonSijm.AnaalIJzer" Version="0.0.7" PrivateAssets="all" />
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

`Architecture.anl` is an XML document. The format originally used the ordinary `.xml` extension, and the XML syntax has not changed: settings still use the `<ArchitecturalLevels>` root, standard XML tooling, and the AnaalIJzer XSD schema.

The custom `.anl` extension gives the settings file an architectural identity instead of making it look like unrelated application data - a generic `Architecture.xml` tends to get filed under "legacy config of uncertain ownership" and removed during a tidy-up sprint. It also gives tools a stable file type to recognize and associate: Arse and the standalone graph editor can be registered as `.anl` handlers, while the Visual Studio companion can recognize an `.anl` file and open it in its dependency-graph editor.

Add an XSD schema hint when you want XML-aware editors to validate element and attribute names while you edit. The schema is [AnaalIjzer.xsd](../src/Main/RonSijm.AnaalIJzer/Scheme/AnaalIjzer.xsd); generated configurations can place a copy beside `Architecture.anl`:

```xml
<ArchitecturalLevels xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                     xsi:noNamespaceSchemaLocation="AnaalIJzer.xsd">
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

The analyzer uses `Architecture.anl` as the explicit top-level settings file convention; other settings files are only read when referenced through `<Include>` or passed directly to Arse.

Skipping this step is the single most common setup mistake. Roslyn never receives the file, the analyzer finds no configuration, and it reports nothing - which looks exactly like a perfectly layered codebase until someone checks.

### 4. Share the same config with `Directory.Build.props`

If several projects should use the same `Architecture.anl`, put the XML next to a solution-level `Directory.Build.props` and register it there instead of copying the file into every project:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="RonSijm.AnaalIJzer" Version="0.0.7" PrivateAssets="all" />
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

[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
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

The analyzer recognizes `AssemblyMetadata("AnaalIJzerSettings", "...")` and reads the second constructor argument as XML. No custom helper attribute or extra package reference is needed.

If both config sources exist, `Architecture.anl` wins and the inline metadata value is ignored without comment, so if carefully crafted inline rules suddenly stopped applying, look for a file someone added last week. Visual Studio and Rider code fixes can now edit both file-based settings and inline `AssemblyMetadata("AnaalIJzerSettings", ...)`, including the owning included file when a rule comes from `<Include>`. The simple one-file examples in this repository use `AssemblyMetadata("AnaalIJzerSettings", ...)`, and exact type-name rules use `nameof(...)` so refactors break the code at compile time instead of quietly breaking the config. Broader examples use XML files when that makes the configuration easier to read. See [IDE code fixes](../docs/configuration/ide-code-fixes.md) for the supported fixer matrix.

**Example project:** [`Example.InlineXml`](../Examples/Features/Example.InlineXml)

That's it. The analyzer activates automatically for every `.cs` file in the project.

Examples use one vocabulary at a time. Explanatory diagnostics use the restaurant roles `Customer`, `Waiter`, `Chef`, and `Pantry`. Setup and reference examples use the technical layers `Presentation`, `Application`, and `Persistence`. A diagram, code block, or explanation never maps one vocabulary onto the other, because a `Waiter` in the `Persistence` layer helps nobody.

```mermaid
flowchart LR
    Customer --> Waiter --> Chef --> Pantry
```

The self-contained projects under [`Examples/`](../Examples/) are referenced inline where their feature is documented. Most intentionally fail with documented `ARCH00X` errors; a few demonstrate clean wildcard config or generated report/documentation output. Scenario examples, such as [`Example.RepositoryQuerySurface`](../Examples/Scenarios/Example.RepositoryQuerySurface), show larger usage patterns rather than a single analyzer feature.

---
