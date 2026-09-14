# Anaaltomy

Anaaltomy measures the compiled C# shape of projects, solutions, or directories and stores aggregate statistics in SQLite. It does not need an `Architecture.anl` file and it does not run AnaalIJzer diagnostics.

```powershell
dotnet tool install --global RonSijm.Anaaltomy
anaaltomy scan --solution .\MySolution.slnx --database .\build\Artifacts\statistics.db
anaaltomy history --repository . --from-root --database .\build\Artifacts\statistics.db
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --group --dimension MemberAccessibility --group-by MemberKind
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --trend --dimension DependencySite --bucket Local
```

`chart` creates PNG reports from SQLite. By default it creates latest-scan bar charts whose titles name the scanned project, solution, or folder; add `--group` and optional `--group-by ...` for grouped latest-scan charts, or add `--trend --dimension ... --bucket ...` for one Git-history line chart.

Use `anaaltomy --help` for the complete command reference.
