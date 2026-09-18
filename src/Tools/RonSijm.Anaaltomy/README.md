# Anaaltomy

Anaaltomy measures the compiled C# shape of projects, solutions, or directories and stores aggregate statistics in SQLite. It does not need an `Architecture.anl` file and it does not run AnaalIJzer diagnostics.

```powershell
dotnet tool install --global RonSijm.Anaaltomy
anaaltomy scan --solution .\MySolution.slnx --database .\build\Artifacts\statistics.db
anaaltomy history --repository . --from-root --database .\build\Artifacts\statistics.db
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --group --dimension MemberAccessibility --group-by MemberKind
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --trend --dimension DependencySite --bucket Local
anaaltomy export --database .\build\Artifacts\statistics.db --format markdown --output .\build\Artifacts\statistics.md
anaaltomy export-database --database .\build\Artifacts\statistics.db --format csv --output-directory .\build\Artifacts\statistics-csv
```

`chart` creates PNG reports from SQLite. By default it creates latest-scan bar charts whose titles name the scanned project, solution, or folder; add `--group` and optional `--group-by ...` for grouped latest-scan charts, or add `--trend --dimension ... --bucket ...` for one Git-history line chart.

`export` writes the latest summary as JSON, CSV, or Markdown. `export-database` writes the full SQLite-shaped store as one file per table using JSON, CSV, or Markdown.

Use `anaaltomy --help` for the complete command reference.
