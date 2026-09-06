## Architecture health

An application can obey every configured edge while its architecture settings quietly drift. `arse inspect` checks both the settings and, when given a project or solution, the code evidence behind them:

```cmd
arse inspect --project src\MyApp\MyApp.csproj --output docs\architecture-health.md --force
arse inspect --solution src\MyApp.slnx --output docs\architecture-health.md --force
arse inspect --solution src\MyApp.slnx --enforce-topology --output build\Artifacts\architecture-health.json --force
arse inspect --config Architecture.anl --force
```

Project validation identifies unclassified and ambiguously classified types, matchers that resolve no current types, stale exceptions, unused allowed edges, configured and observed dependency cycles, and current analyzer violations. Unused edges and dead matchers are the configuration equivalent of unreachable code: harmless until somebody reads them as a statement of intent. Solution validation runs the same checks for every C# project and writes one combined report. Add `--enforce-topology` to evaluate configured solution-level module edges as `TOPO001` and configured module cycles as `TOPO002`. XML-only validation checks configuration validity and configured cycles without loading MSBuild. An `.json` output path writes the same ordered findings as machine-readable evidence.

**Example project:** [`Example.ArchitectureHealth`](../Examples/Features/Example.ArchitectureHealth)

---
