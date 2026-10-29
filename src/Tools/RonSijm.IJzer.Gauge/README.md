# IJzer Gauge

IJzer Gauge measures the compiled C# shape of projects, solutions, or directories and stores aggregate statistics in SQLite. It does not need an `Architecture.anl` file and it does not run IJzer diagnostics.

```powershell
dotnet tool install --global RonSijm.IJzer.Gauge
ijzer-gauge scan --solution .\MySolution.slnx --database .\build\Artifacts\statistics.db
ijzer-gauge history --repository . --from-root --database .\build\Artifacts\statistics.db
ijzer-gauge chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts
ijzer-gauge chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --group --dimension MemberAccessibility --group-by MemberKind
ijzer-gauge chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --trend --dimension DependencySite --bucket Local
ijzer-gauge export --database .\build\Artifacts\statistics.db --format markdown --output .\build\Artifacts\statistics.md
ijzer-gauge export-database --database .\build\Artifacts\statistics.db --format csv --output-directory .\build\Artifacts\statistics-csv
```

`chart` creates PNG reports from SQLite. By default it creates latest-scan bar charts whose titles name the scanned project, solution, or folder; add `--group` and optional `--group-by ...` for grouped latest-scan charts, or add `--trend --dimension ... --bucket ...` for one Git-history line chart.

`export` writes the latest summary as JSON, CSV, or Markdown. `export-database` writes the full SQLite-shaped store as one file per table using JSON, CSV, or Markdown.

Use `ijzer-gauge --help` for the complete command reference.
