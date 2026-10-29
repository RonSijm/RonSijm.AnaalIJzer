# IJzer Forge

Interactive and scriptable architecture tooling for .NET.

IJzer Forge is the interactive and scriptable tool for `RonSijm.IJzer`. Both modes use the same operation catalog and `ApplicationRunner`, so they have identical capabilities. Interactive architecture inspection previews the report before offering `Save` and an output path.

Install the .NET tool:

```cmd
dotnet tool install --global RonSijm.IJzer.Forge
```

Run `ijzer` without arguments to open the RazorConsole interface. Path fields suggest matching directories and files while you type. Use Up/Down to select a suggestion, Right Arrow to complete it in place, or Tab to apply the selected or shared-prefix completion. Use commands for headless automation:

```cmd
ijzer generate-config --project path\to\Project.csproj
ijzer generate-config --solution path\to\Solution.slnx --strategy helpful --output Architecture.anl
ijzer generate-config --project path\to\Project.csproj --strategy conventions --minimum-confidence 0.95 --minimum-support 10 --generate-documentation --include-input
ijzer export-config --project path\to\Project.csproj
ijzer documentation --project path\to\Project.csproj --include-code-evidence --include-input
ijzer documentation --config path\to\Architecture.anl --include-input
ijzer report --project path\to\Project.csproj
ijzer report --solution path\to\Solution.slnx
ijzer inspect --project path\to\Project.csproj
ijzer inspect --solution path\to\Solution.slnx
ijzer inspect --config path\to\Architecture.anl
ijzer fixes --project path\to\Project.csproj
ijzer fixes --solution path\to\Solution.slnx --output docs\architecture-fixes.md
ijzer apply-fix --project path\to\Project.csproj --fix-id fix-abc123
ijzer merge-config --config Shared.anl --config Project.anl --output Architecture.anl
ijzer split-config --config Architecture.anl --output ArchitectureRules
ijzer associate-anl
ijzer unassociate-anl
```

`inspect` writes an architecture-health report and exits with code `3` when it finds issues. Its aliases are `validate`, `doctor`, and `health`. Use `--solution` when the architecture spans multiple projects.

`fixes` lists configuration-backed proposals from the same Roslyn fixer catalog used by the IDE. `apply-fix` applies one proposal id and then refreshes the remaining proposal list.

`associate-anl` registers `.anl` files for the current Windows user so opening one runs `ijzer inspect --config "%1"`. `unassociate-anl` removes only IJzer Forge's own `.anl` association.

Run `ijzer --help` for all options and aliases. `ijzer tui` explicitly starts the interactive interface.
