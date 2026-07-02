# Example.DocumentationDemo

This example is intentionally XML-backed and verbose. It demonstrates `enableDocumentation`, `enforceAcyclic`, descriptions, includes, class/namespace/assembly matchers, forbidden rules, wildcard rules, allowed and blocked dependency edges, site allow/block lists, and multiple unrelated dependency chains in one project.

Run `GenerateDocumentation.bat` to generate the committed documentation directly from this example's `ArchitecturalLevels.xml` file. The script resolves its paths relative to its own location, so its working directory does not matter.

The equivalent command from the repository root is:

```cmd
dotnet run --project src\Tools\RonSijm.AnaalIJzer.Ridder -- documentation --config Examples\Documentation\Example.DocumentationDemo\ArchitecturalLevels.xml --force
```
