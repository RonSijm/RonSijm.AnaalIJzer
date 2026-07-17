# Arse

**A**rchitecture **R**ule **S**tandalone **E**xecutable.

Arse is the interactive and scriptable tool for `RonSijm.AnaalIJzer`. Both modes use the same operation catalog and `ToolRunner`, so they have identical capabilities. Interactive architecture inspection previews the report before offering `Save` and an output path.

Install the .NET tool:

```cmd
dotnet tool install --global RonSijm.AnaalIJzer.Arse
```

Run `arse` without arguments to open the RazorConsole interface. Path fields suggest matching directories and files while you type. Use Up/Down to select a suggestion, Right Arrow to complete it in place, or Tab to apply the selected or shared-prefix completion. Use commands for headless automation:

```cmd
arse generate-config --project path\to\Project.csproj
arse generate-config --solution path\to\Solution.slnx --strategy helpful --output Architecture.anl
arse generate-config --project path\to\Project.csproj --strategy conventions --minimum-confidence 0.95 --minimum-support 10 --generate-documentation --include-input
arse export-config --project path\to\Project.csproj
arse documentation --project path\to\Project.csproj --include-code-evidence --include-input
arse documentation --config path\to\Architecture.anl --include-input
arse report --project path\to\Project.csproj
arse report --solution path\to\Solution.slnx
arse inspect --project path\to\Project.csproj
arse inspect --solution path\to\Solution.slnx
arse inspect --config path\to\Architecture.anl
arse merge-config --config Shared.anl --config Project.anl --output Architecture.anl
arse split-config --config Architecture.anl --output ArchitectureRules
arse associate-anl
arse unassociate-anl
```

`inspect` writes an architecture-health report and exits with code `3` when it finds issues. Its aliases are `validate`, `doctor`, and `health`. Use `--solution` when the architecture spans multiple projects.

`associate-anl` registers `.anl` files for the current Windows user so opening one runs `arse inspect --config "%1"`. `unassociate-anl` removes only Arse's own `.anl` association.

Run `arse --help` for all options and aliases. `arse tui` explicitly starts the interactive interface.
