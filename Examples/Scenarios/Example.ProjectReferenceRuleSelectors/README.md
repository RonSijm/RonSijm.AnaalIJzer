# Example.ProjectReferenceRuleSelectors

This scenario demonstrates precise `ProjectArchitecture` rules without turning every
project into its own `ProjectGroup`.

`Example.ProjectReferenceRuleSelectors.Orders.Application` references two contract
projects. The configuration permits the ordering contract and rejects the payments
contract with `ARCH_PROJ_001`:

```text
Orders.Application -> Orders.Contracts    allowed
Orders.Application -> Payments.Contracts  ARCH_PROJ_001
```

The rule keeps the useful broad groups (`Application` and `Contracts`) but narrows one
edge with exact `From` and `To` project selectors. A selected source project must use a
selected target project; unrelated projects in the same groups are not accidentally put
into allowlist mode.

Expected Release result:

- `Example.ProjectReferenceRuleSelectors.Orders.Application`: `ARCH_PROJ_001 = 1`
- `Example.ProjectReferenceRuleSelectors.Orders.Contracts`: clean
- `Example.ProjectReferenceRuleSelectors.Payments.Contracts`: clean
