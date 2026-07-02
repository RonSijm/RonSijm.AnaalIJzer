# Ridder

**R**ule **I**nspector for **D**ependency **D**iagrams, **E**xceptions, and **R**eports.

Ridder is the interactive and scriptable tool for `RonSijm.AnaalIJzer`. Both modes use the same operation catalog and `ToolRunner`, so they have identical capabilities. Interactive architecture inspection previews the report before offering `Save` and an output path.

Install the .NET tool:

```cmd
dotnet tool install --global RonSijm.AnaalIJzer.Ridder
```

Run `ridder` without arguments to open the RazorConsole interface. Use commands for headless automation:

```cmd
ridder generate-config --project path\to\Project.csproj
ridder generate-config --project path\to\Project.csproj --strategy conventions --minimum-confidence 0.95 --minimum-support 10 --generate-documentation --include-input
ridder export-config --project path\to\Project.csproj
ridder documentation --project path\to\Project.csproj --include-code-evidence --include-input
ridder documentation --config path\to\ArchitecturalLevels.xml --include-input
ridder report --project path\to\Project.csproj
ridder inspect --project path\to\Project.csproj
ridder inspect --config path\to\ArchitecturalLevels.xml
ridder merge-config --config Shared.xml --config Project.xml --output ArchitecturalLevels.xml
ridder split-config --config ArchitecturalLevels.xml --output ArchitectureRules
```

`inspect` writes an architecture-health report and exits with code `3` when it finds issues. Its aliases are `validate`, `doctor`, and `health`.

Run `ridder --help` for all options and aliases. `ridder tui` explicitly starts the interactive interface.
