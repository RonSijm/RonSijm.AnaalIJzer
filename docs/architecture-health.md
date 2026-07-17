## Architecture health

An application can obey every configured edge while its architecture settings quietly drift. `arse inspect` checks both the settings and, when given a project or solution, the code evidence behind them:

```cmd
arse inspect --project src\MyApp\MyApp.csproj --output docs\architecture-health.md --force
arse inspect --solution src\MyApp.slnx --output docs\architecture-health.md --force
arse inspect --config Architecture.anl --force
```

Project validation identifies unclassified and ambiguously classified types, matchers that resolve no current types, stale exceptions, unused allowed edges, configured and observed dependency cycles, and current analyzer violations. Solution validation runs the same checks for every C# project and writes one combined report. XML-only validation checks configuration validity and configured cycles without loading MSBuild.

**Example project:** [`Example.ArchitectureHealth`](../Examples/Features/Example.ArchitectureHealth)

---
