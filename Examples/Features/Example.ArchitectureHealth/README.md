# Architecture Health Example

This project intentionally contains a permitted dependency cycle, an unused edge, an unmatched layer matcher, a stale exception, and an unclassified type.

Run:

```powershell
arse inspect --project .\Example.ArchitectureHealth.csproj --output .\architecture-health.md --force
```

The analyzer itself reports no dependency violation because both observed directions are allowed. The health report highlights configuration drift and structural risks that are not individual illegal dependency sites.
