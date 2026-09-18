## ARCH_OPCT_008 - Operation-contract shape mismatch

`ARCH_OPCT_008` means a selected owner or entry point returns a type that does not match the operation's configured `<Response>` shape.

The diagnostic is attached to the selected declaration and records whether the owner or entry point failed the response check.

There is no automatic code fix because converting a response contract can require mapping, error handling, and domain-specific data selection.

**Example:** [`Example.Arch_OPCT_008.ResponseShapeMismatch`](../../Examples/Diagnostics/OPCT/Example.Arch_OPCT_008.ResponseShapeMismatch)

### Real-world uses

- Keep controllers, consumers, and application owners aligned on one explicit response contract.
- Prevent an operation owner from leaking a raw persistence or framework response type.
