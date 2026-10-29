## Violation report

In addition to inline diagnostics, IJzer Forge can write a Markdown summary of every violation it finds. Enable a default path by setting `enableReport="true"` on the `<ArchitecturalLevels>` root and optionally `reportPath`, or pass `--output` directly:

```xml
<ArchitecturalLevels enableReport="true"
                     reportPath="../../docs/architectural-violations.md">
  …
</ArchitecturalLevels>
```

```cmd
ijzer report --project src\MyApp\MyApp.csproj --force
ijzer report --solution src\MyApp.slnx --output docs\architectural-violations.md --force
```

The report does a few specific things:

- Groups code dependency, type-policy, and name-rule violations by their exact diagnostic IDs.
- Adds a **Suggested Configuration** block for `ARCH_DEP_002`, with `<Layer>` and `<AllowedDependency>` snippets for the unrecognized dependencies it found.
- Accepts `--project` for one assembly or `--solution` for an architecture spread across several projects.
- Leaves configuration findings and cycles to the `inspect` health report.

It is a violation report, not a second health report with a different filename.

- **CI dashboards** - commit the report as a build artifact and diff it across runs to track architectural drift.
- **Onboarding** - point new contributors at a single file that summarizes the project's layering health.
- **Bootstrapping** - start with `requireRecognizedDependencies="Constructor"` and `enableReport="true"` on a legacy codebase, copy the suggested `<Layer>` snippets into the config, then add more sites deliberately.

The report is written by `RonSijm.IJzer.Reporting.ArchitecturalViolationReporter`. IJzer Forge runs the analyzer in-process with Roslyn, converts the resulting diagnostics into report rows, and writes the file explicitly. Normal analyzer builds do not perform filesystem I/O, because an analyzer that writes files during a parallel build is a support ticket waiting to be filed.

Assembly-metadata failures (`ARCH_ASSM_001`) are reported in a dedicated table with the current assembly, emitted attribute type, matching policy rule, and reason. The table includes project-file-generated attributes such as `InternalsVisibleTo` even when they do not map to a handwritten source location.

### Example report

This repository ships a [rendered example report](../Examples/Documentation/Generated/architectural-violations.md). It comes from [`Examples/Documentation/Example.ReportDemo`](../Examples/Documentation/Example.ReportDemo), which intentionally contains one violation of each diagnostic ID.

Regenerate it from the repo root:

```cmd
dotnet run --project src\Tools\RonSijm.IJzer.Forge -- report --project Examples\Documentation\Example.ReportDemo\Example.ReportDemo.csproj --force
```

**In your own codebase**, install the tool and run `ijzer report --project path\to\Project.csproj` or `ijzer report --solution path\to\Solution.slnx`. Pass `--output` to override the default path, and `--force` to overwrite an existing file.

---
