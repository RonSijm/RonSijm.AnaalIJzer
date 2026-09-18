# ARCH_OPER_001: One Selected Environment Member

The policy forbids only `Environment.MachineName`, not the whole `System.Environment` type. `Environment.NewLine` remains allowed, showing why selected-operation policies are narrower than broad type policies.

```cmd
dotnet build Examples\Diagnostics\OPER\Example.Arch_OPER_001.SelectedEnvironmentMember -c Release
```
