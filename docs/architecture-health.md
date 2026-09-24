## Architecture health

An application can obey every configured edge while its architecture settings quietly drift. `arse inspect` checks both the settings and, when given a project or solution, the code evidence behind them:

```cmd
arse inspect --project src\MyApp\MyApp.csproj --output docs\architecture-health.md --force
arse inspect --solution src\MyApp.slnx --output docs\architecture-health.md --force
arse inspect --solution src\MyApp.slnx --enforce-topology --output build\Artifacts\architecture-health.json --force
arse inspect --config Architecture.anl --force
```

The input decides how far inspection goes:

- **One `.anl` file** checks configuration validity and configured cycles without loading MSBuild.
- **One project** also checks:
  - unclassified or ambiguously classified types;
  - matchers that resolve no current types;
  - stale exceptions and unused allowed edges;
  - configured and observed dependency cycles;
  - current analyzer violations.
- **One solution** runs the project checks for every C# project and writes one combined report.
  - Add `--enforce-topology` to report configured module edges as `ARCH_SOL_001` and module cycles as `ARCH_SOL_006`.
- **A `.json` output path** writes the same ordered findings as machine-readable evidence.

Unused edges and dead matchers are the configuration equivalent of unreachable code: harmless until somebody reads them as a statement of intent.

**Example project:** [`Example.ArchitectureHealth`](../Examples/Features/Example.ArchitectureHealth)

---
