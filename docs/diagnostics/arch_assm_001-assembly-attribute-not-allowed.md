## ARCH_ASSM_001 - Assembly attribute policy violation

`ARCH_ASSM_001` means an attribute emitted on the current assembly matches a root-level `<AssemblyAttributePolicy>` rule that does not permit it.

The policy examines final semantic assembly metadata. It therefore catches both a C# declaration such as `[assembly: InternalsVisibleTo("OtherAssembly")]` and an SDK item such as `<InternalsVisibleTo Include="OtherAssembly" />` that produces the same attribute during compilation.

The diagnostic identifies the current assembly, the fully qualified attribute type, the matching policy rule, and the policy reason. SDK-generated attributes may not have a useful source location; the diagnostic remains a compilation result because the forbidden metadata is still real.

### Real-world uses

- Limit `InternalsVisibleTo` grants to approved test, migration, or companion assemblies.
- Prevent a compliance, runtime, or plugin-registration attribute from being attached with an unapproved argument value.
- Require selected assembly metadata attributes to use a known publisher, capability, or environment value.
- Keep equivalent handwritten and SDK-generated assembly metadata under one policy instead of maintaining separate source and project-file checks.

There is deliberately no automatic code fix. The analyzer can identify the rejected metadata, but it cannot decide whether to remove an assembly friend, change the project setting that generated it, or widen the policy.

**Examples:**

- [`Example.Arch_ASSM_001.AssemblyAttributePolicy.Code`](../../Examples/Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Code)
- [`Example.Arch_ASSM_001.AssemblyAttributePolicy.Project`](../../Examples/Diagnostics/ASSM/Example.Arch_ASSM_001.AssemblyAttributePolicy.Project)
