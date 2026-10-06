# IaC operation policies

This generator expresses the clock-access rule from [`Example.Arch_OPER_001.ClockAccess`](../../Diagnostics/OPER/Example.Arch_OPER_001.ClockAccess) through the IaC object model.

- The rule finds static property reads on `System.DateTime`.
- Three operation matchers cover `UtcNow`, `Now`, and `Today`.
- The containing type, member kind, operation kind, and dependency site are all typed C# values where the configuration vocabulary has a finite set of choices.

Generate the committed configuration:

```cmd
dotnet run --project Example.InfrastructureAsCode.OperationPolicies.csproj -- Architecture.anl
```

The generated [`Architecture.anl`](Architecture.anl) can be consumed without deploying the IaC package with the analyzed application.
