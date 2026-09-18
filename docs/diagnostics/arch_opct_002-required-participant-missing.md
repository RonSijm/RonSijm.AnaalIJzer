## ARCH_OPCT_002 - Required operation-contract participant missing

`ARCH_OPCT_002` means a method selected by an explicit root-level `<Operations>` rule is missing a configured request parameter or a selected entry point does not invoke the configured owner.

| Violation kind | Meaning |
| --- | --- |
| `OwnerMissingRequest` | The selected owner has no parameter matching `<Request>`. |
| `EntryPointMissingRequest` | The selected entry point has no parameter matching `<Request>`. |
| `EntryPointDoesNotInvokeOwner` | The selected entry point does not directly call the selected owner in its own body. |

The diagnostic properties include the operation name, participant role, violation kind, and configuration location. A separate workspace finding covers missing or ambiguous owners across a project or solution.

There is deliberately no automatic code fix. The analyzer can show which declared source fact is missing, but it cannot safely decide which service should own a workflow or how a response should be reshaped.

**Example:** [`Example.Arch_OPCT_002.RequiredOwnerInvocation`](../../Examples/Diagnostics/OPCT/Example.Arch_OPCT_002.RequiredOwnerInvocation)

### Real-world uses

- Require a web endpoint, scheduled job, or message consumer to call the designated application operation with the intended request and response shapes.
- Prevent a workflow from quietly moving into a controller or worker when the configured application owner is supposed to remain its single entry point.
