### ARCH_OPER_011 - Operation cardinality

Reported when a selected declaration contains more occurrences of an operation than a configured `<MaximumOperationCount>` permits.

The diagnostic is attached to the first occurrence beyond the maximum, so the highlighted source is the operation that made the count invalid.

Typical responses are to remove an accidental duplicate, introduce an idempotent workflow, narrow the matcher, or intentionally revise the maximum.

There is no automatic code fix because deciding which occurrence is redundant is a domain decision.

See [behavioral operation policies](../configuration/behavioral-operation-policies.md) for matcher and counting semantics.

**Focused example:** [`Example.Arch_OPER_011.MaximumOperationCount`](../../Examples/Diagnostics/OPER/Example.Arch_OPER_011.MaximumOperationCount).

#### Real-world uses

- Ensure a payment, message publication, or transaction commit happens at most once.
- Prevent duplicate audit writes or repeated calls to a non-idempotent external operation.
