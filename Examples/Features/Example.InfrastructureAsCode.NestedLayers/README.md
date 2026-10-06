# IaC nested layers

This generator is a compact infrastructure-as-code counterpart to [`Example.NestedLayers`](../Example.NestedLayers).

- `Ordering` and `Billing` are top-level boundaries.
- Their child layers reuse names such as `Application`, `Repository`, `Contracts`, and `Implementation` within local scope.
- The cross-boundary dependency has an outer gate plus explicit ordering egress and billing ingress gates.
- The implementation-to-contract edge is restricted to interface implementation sites.

Generate the committed configuration:

```cmd
dotnet run --project Example.InfrastructureAsCode.NestedLayers.csproj -- Architecture.anl
```

The generated [`Architecture.anl`](Architecture.anl) preserves the same element order as the C# model.
