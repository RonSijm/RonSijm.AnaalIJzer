# Diagnostic Examples

Each diagnostic example is grouped by the concern segment in its semantic diagnostic ID. For example, `ARCH_DEP_001` examples live under [`DEP/`](DEP), while `ARCH_OPER_001` examples live under [`OPER/`](OPER).

The project directory keeps the complete diagnostic ID and case name:

```text
Diagnostics/<CONCERN>/Example.Arch_<CONCERN>_<REASON>.<CaseName>/
```

| Concern | Examples cover |
| ------- | -------------- |
| [`API`](API) | Public API exposure and transitive exposure |
| [`ASSM`](ASSM) | Assembly-level policy |
| [`BOUND`](BOUND) | Architectural boundary entry points |
| [`CONF`](CONF) | Invalid or cyclic configuration |
| [`CONT`](CONT) | Contract shape and purity |
| [`DEP`](DEP) | Dependency direction, recognition, scope, and cycles |
| [`INH`](INH) | Required and forbidden inheritance |
| [`NS`](NS) | Namespace hierarchy boundaries |
| [`OPCT`](OPCT) | Operation contracts and participants |
| [`OPER`](OPER) | Forbidden, required, ordered, and cardinality-limited operations |
| [`RET`](RET) | Return-value policies |
| [`TYPE`](TYPE) | Type allowlists and blocklists |
| [`VIS`](VIS) | Declaration visibility policies |

See the [examples index](../README.md#diagnostics) for every project and its documentation link.
