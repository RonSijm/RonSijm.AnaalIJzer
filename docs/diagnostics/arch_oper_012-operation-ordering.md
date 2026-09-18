### ARCH_OPER_012 - Operation ordering

Reported when a declaration violates `<RequiredOperationBefore>` or `<ForbiddenOperationAfter>`.

The diagnostic is attached to the selected operation at the invalid position: for example, a save without prior validation or a mutation after a terminal commit.

Typical responses are to move or add the required earlier operation, remove a forbidden later operation, or narrow the declaration and operation matchers.

There is no automatic code fix because reordering side effects can change program behavior.

See [behavioral operation policies](../configuration/behavioral-operation-policies.md), especially the difference between dominance and lexical ordering.

**Focused examples:** [`Example.Arch_OPER_012.RequiredOperationBefore`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_012.RequiredOperationBefore) and [`Example.Arch_OPER_012.ForbiddenOperationAfter`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_012.ForbiddenOperationAfter).

#### Real-world uses

- Require authorization or validation before persistence, publication, or payment.
- Prevent source mutations, logging, or outbound calls after a configured terminal operation.
