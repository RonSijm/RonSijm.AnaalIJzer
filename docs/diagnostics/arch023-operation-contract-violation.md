## ARCH023 - Operation contract violation

`ARCH023` means a method selected by an explicit root-level `<Operations>` rule does not satisfy its declared source contract.

| Violation kind | Meaning |
| --- | --- |
| `OwnerOutsideAllowedLayer` | The selected owner method's containing type is outside `allowedOwnerLayers`. |
| `OwnerMissingRequest` | The selected owner has no parameter matching `<Request>`. |
| `OwnerInvalidResponse` | The selected owner's direct return type does not match `<Response>`. |
| `EntryPointOutsideAllowedLayer` | The selected entry point's containing type is outside `allowedEntryPointLayers`. |
| `EntryPointMissingRequest` | The selected entry point has no parameter matching `<Request>`. |
| `EntryPointInvalidResponse` | The selected entry point's direct return type does not match `<Response>`. |
| `EntryPointDoesNotInvokeOwner` | The selected entry point does not directly call the selected owner in its own body. |

The diagnostic properties include the operation name, participant role, violation kind, and configuration location. A separate workspace finding covers missing or ambiguous owners across a project or solution.

There is deliberately no automatic code fix. The analyzer can show which declared source fact is missing, but it cannot safely decide which service should own a workflow or how a response should be reshaped.

**Example:** [`Example.Arch023.OperationContract`](../../Examples/Diagnostics/Example.Arch023.OperationContract)

### Real-world uses

- Require a web endpoint, scheduled job, or message consumer to call the designated application operation with the intended request and response shapes.
- Prevent a workflow from quietly moving into a controller or worker when the configured application owner is supposed to remain its single entry point.
