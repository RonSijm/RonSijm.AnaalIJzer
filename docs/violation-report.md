## Violation report

In addition to inline diagnostics, Arse can write a Markdown summary of every violation it finds. Enable a default path by setting `enableReport="true"` on the `<ArchitecturalLevels>` root and optionally `reportPath`, or pass `--output` directly:

```xml
<ArchitecturalLevels enableReport="true"
                     reportPath="../../docs/architectural-violations.md">
  …
</ArchitecturalLevels>
```

```cmd
arse report --project src\MyApp\MyApp.csproj --force
arse report --solution src\MyApp.slnx --output docs\architectural-violations.md --force
```

The violation report groups code dependency violations by diagnostic ID (ARCH001/002/003/004/005) and, for ARCH002, includes a **Suggested Configuration** block with `<Layer>` and `<AllowedDependency>` snippets that would resolve the unrecognized dependencies it found. Use `--project` for one assembly or `--solution` when the architecture is enforced across multiple projects. Configuration findings and cycles belong in the `inspect` health report.

- **CI dashboards** - commit the report as a build artifact and diff it across runs to track architectural drift.
- **Onboarding** - point new contributors at a single file that summarizes the project's layering health.
- **Bootstrapping** - start with `requireRecognizedDependencies="Constructor"` and `enableReport="true"` on a legacy codebase, copy the suggested `<Layer>` snippets into the config, then add more sites deliberately.

The report is written by `RonSijm.AnaalIJzer.Reporting.ArchitecturalViolationReporter`. Arse runs the analyzer in-process with Roslyn, converts the resulting diagnostics into report rows, and writes the file explicitly. Normal analyzer builds do not perform filesystem I/O.

### Example report

This repository ships a [rendered example report](../Examples/Documentation/Generated/architectural-violations.md) generated from the [`Examples/Documentation/Example.ReportDemo`](../Examples/Documentation/Example.ReportDemo) project, which intentionally contains one violation of each diagnostic ID. To regenerate it from the repo root:

```cmd
dotnet run --project src\Tools\RonSijm.AnaalIJzer.Arse -- report --project Examples\Documentation\Example.ReportDemo\Example.ReportDemo.csproj --force
```

**In your own codebase**, install the tool and run `arse report --project path\to\Project.csproj` or `arse report --solution path\to\Solution.slnx`. Pass `--output` to override the default path, and `--force` to overwrite an existing file.

---
