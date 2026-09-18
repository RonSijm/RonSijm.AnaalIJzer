## ARCH_OPCT_001 - Operation-contract participant not allowed

`ARCH_OPCT_001` means a selected owner or entry-point method belongs to a layer that is not listed in `allowedOwnerLayers` or `allowedEntryPointLayers`.

The diagnostic is attached to the selected declaration and includes the operation name, participant role, effective layer, and configuration location.

There is no automatic code fix because moving a declaration or changing an operation's ownership is an explicit architecture decision.

**Example:** [`Example.Arch_OPCT_001.ParticipantNotAllowed`](../../Examples/Diagnostics/OPCT/Example.Arch_OPCT_001.ParticipantNotAllowed)

### Real-world uses

- Keep HTTP controllers as operation entry points while application services remain the owners.
- Prevent infrastructure or presentation code from becoming the configured owner of a business operation.
