# Anaaltomy Statistics

Anaaltomy is a standalone compiled-code statistics tool. It measures the shape of C# projects with Roslyn and keeps the results in SQLite so the same measurements can be compared across the current tree and Git history.

It is deliberately separate from AnaalIJzer's architecture rules:

- It does not need `Architecture.anl`.
- It does not run `ARCHxxx` diagnostics or classify code into layers.
- It reuses the same definitions of C# type kinds and dependency sites, so its measurements mean the same thing as the analyzer's terminology.

## Install

```powershell
dotnet tool install --global RonSijm.Anaaltomy
anaaltomy --help
```

The global command is `anaaltomy`.

To build the tool and its NuGet package locally from this repository, run:

```cmd
build\Scripts\Anaaltomy\build-anaaltomy.bat
```

The compiled tool is written to `build\Artifacts\Anaaltomy`. The global-tool packages are written to `build\Artifacts\Anaaltomy\Packages`, ready for local installation or manual upload to NuGet.org.

## Scan The Current Tree

Choose exactly one input shape and an explicit SQLite database path. A sensible database location is under `build\Artifacts`, rather than beside source files.

```powershell
anaaltomy scan --project .\src\Pizza\Pizza.csproj --database .\build\Artifacts\statistics.db
anaaltomy scan --solution .\Pizza.slnx --database .\build\Artifacts\statistics.db
anaaltomy scan --directory .\src --database .\build\Artifacts\statistics.db
```

`scan` loads real C# project compilations through `MSBuildWorkspace`. It understands project references, conditional compilation, language versions, linked files, and target frameworks instead of guessing from raw text.

Useful scan options:

| Option | Default | Meaning |
|---|---:|---|
| `--configuration Release` | `Release` | MSBuild configuration used while loading projects. |
| `--framework net10.0` | project-selected | Select a target framework for a multi-target project. |
| `--include-generated` | off | Include generated source files. |
| `--restore-mode auto\|never\|always` | `auto` | Control restore before project evaluation. |
| `--allow-partial` | off | Return exit code `0` even when a project could only be partially scanned. |

Compiler errors do not discard usable observations. Anaaltomy stores the available measurements and marks the affected project and scan as partial. It exits with code `3` unless `--allow-partial` is supplied.

## What It Measures

Each scan records aggregate counts, not a database row for every source location:

| Dimension | Examples | Unit |
|---|---|---|
| `TypeKind` | `Class`, `Record`, `RecordStruct`, `Interface`, `Enum` | logical source type |
| `DependencySite` | `Constructor`, `MethodReturn`, `Local`, `Inheritance`, `Attribute` | recognized source occurrence |
| `TypeAccessibility` | `Public`, `Internal`, `File` | logical source type |
| `MemberAccessibility` | `Public`, `Protected`, `Private` | declared source member |
| `MemberKind` | `Constructor`, `Method`, `Property`, `Field`, `Event` | logical source member |

Partial types are counted once as logical types. Dependency sites are counted per source occurrence, including both a generic use and its nested generic arguments when applicable. Directory and solution totals are per compilation: a linked file compiled by two projects appears once in each project compilation.

Generated code is excluded by default. Metadata/framework types are not counted as project declarations, and unresolved compiler observations are recorded as partial-scan metadata instead of being forced into an invented bucket.

## Store And Query Results

SQLite is the canonical store. JSON and CSV are export formats, not the source of truth.

```powershell
anaaltomy summary --database .\build\Artifacts\statistics.db
anaaltomy trend --database .\build\Artifacts\statistics.db --dimension DependencySite --bucket Local
anaaltomy compare --database .\build\Artifacts\statistics.db --from v0.2.0 --to HEAD
anaaltomy commits --database .\build\Artifacts\statistics.db --dimension TypeKind --bucket Record
anaaltomy export --database .\build\Artifacts\statistics.db --format json --output .\build\Artifacts\statistics.json
anaaltomy export --database .\build\Artifacts\statistics.db --format csv --output .\build\Artifacts\statistics.csv
anaaltomy export --database .\build\Artifacts\statistics.db --format markdown --output .\build\Artifacts\statistics.md
anaaltomy export-database --database .\build\Artifacts\statistics.db --format json --output-directory .\build\Artifacts\statistics-json
anaaltomy export-database --database .\build\Artifacts\statistics.db --format csv --output-directory .\build\Artifacts\statistics-csv
anaaltomy export-database --database .\build\Artifacts\statistics.db --format markdown --output-directory .\build\Artifacts\statistics-md
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --dimension DependencySite
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --group --dimension MemberAccessibility --group-by MemberKind
anaaltomy chart --database .\build\Artifacts\statistics.db --output-directory .\build\Artifacts\charts --trend --dimension DependencySite --bucket Local
```

The query commands answer different questions:

- `trend`: how did one dimension/bucket change over commit time?
- `commits`: at which commits did that stored count actually change?
- `compare`: what is the bucket-by-bucket difference between two stored commit scans?

They use the most recently completed repository/scan-definition history in the database. Measurements collected with different compiler options are never quietly combined into one very confident graph.

The export commands are deliberately separate:

- `export` writes the latest summary as JSON, CSV, or Markdown.
- `export-database` writes the full SQLite-shaped store as one file per table:
  - `SchemaVersion`, `Repository`, `GitCommit`, and `GitCommitParent`;
  - `ScanDefinition`, `CommitScan`, and `ProjectScan`;
  - `Measurement`, `GroupedMeasurement`, and `ScanFailure`.

SQLite remains the source of truth. The exported files are portable snapshots for reporting, inspection, or downstream tooling.

`chart` creates deterministic PNG reports:

- Without `--trend`, it uses the latest scan.
- Without `--dimension`, it writes one horizontal bar chart for every populated dimension.
  - Type kinds, dependency sites, type accessibility, member accessibility, and member kinds each get their own chart.
- Every title names the scanned project, solution, or folder.
  - For example: `Anaaltomy Dependency Site breakdown of 'Azure.Storage.Blobs'`.
- `--group` creates a grouped breakdown from the latest scan.
  - Use `--group-by <dimension>` to choose the grouping dimension explicitly.
- `--trend --dimension <dimension> --bucket <bucket>` creates a line chart across stored Git-history points.

The database remains authoritative. PNG files are the part you can put in a report without asking its readers to query SQLite first.

An exported JSON summary looks like this in principle:

```json
{
  "projectCount": 3,
  "failureCount": 0,
  "measurements": [
    { "dimension": "TypeKind", "bucket": "Class", "count": 42 },
    { "dimension": "DependencySite", "bucket": "Local", "count": 107 }
  ],
  "groupedMeasurements": [
    { "dimension": "MemberAccessibility", "bucket": "Public", "groupDimension": "MemberKind", "groupBucket": "Method", "count": 30 }
  ]
}
```

The database starts with an explicit schema-version table and keeps repositories, commits, every parent edge, scan definitions, project scans, measurements, grouped measurements, and failures. This preserves merge-parent relationships rather than flattening Git history into a guessed linear sequence.

## Scan Git History Safely

Historical scans use a temporary detached Git worktree outside your active checkout. Anaaltomy never checks out a historical revision in the worktree you are using to write code.

Scanning all reachable history must be explicit:

```powershell
anaaltomy history --repository . --from-root --database .\build\Artifacts\statistics.db
anaaltomy history --repository . --from v0.2.0 --to HEAD --database .\build\Artifacts\statistics.db
anaaltomy history --repository . --from-root --first-parent --max-commits 50 --database .\build\Artifacts\statistics.db
```

History selection is explicit:

- Use either `--from-root` or `--from <revision>`.
  - The tool rejects an unbounded accidental history scan.
- Add `--first-parent` to follow the primary integration path.
- Without it, Anaaltomy scans every reachable commit in topological order.
- Merge commits are scanned as their resulting source tree and retain both parent links in SQLite.

Historical scans default to `--restore-mode always`, because an isolated worktree may not have restored assets. They can be resumed:

```powershell
anaaltomy history --repository . --from-root --resume --database .\build\Artifacts\statistics.db
```

Completed commit scans with the same scan definition are skipped; incomplete or failed commits are retried. Anaaltomy records a failure and continues when it can, returning exit code `3` unless `--allow-partial` was requested.

## Safety And Exit Codes

`MSBuildWorkspace` project evaluation and `dotnet restore` can execute repository-controlled build logic. Only scan repositories you trust, or run untrusted repositories inside a suitable sandbox or disposable environment.

| Exit code | Meaning |
|---:|---|
| `0` | Complete scan, successful query, or successful export. |
| `2` | Invalid command-line input. |
| `3` | Partial scan, unresolved project, or failed historical commit. |
| `4` | SQLite migration or persistence failure. |

Use `anaaltomy --help` for the complete option list.
