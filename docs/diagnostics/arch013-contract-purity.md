### ARCH013 - Contract purity violation

Reported when a source declaration belongs to a layer with an applicable `<ContractPolicy>` and its declaration shape does not pass that policy. A contract that has acquired setters, state, and a method body is an implementation wearing a contract's job title.

Example:

```text
'Name' (layer Contracts) violates contract purity at DisallowedPropertyAccessor:
the ContractPolicy in layer 'Contracts' allows only property accessors Get
```

The diagnostic is reported on the offending member, accessor, body, or type identifier, depending on the failing rule.

Diagnostic properties include:

- `CallerTypeName`
- `CallerLayerName`
- `DeclaredSymbolName`
- `ContractViolationKind`
- `ViolationReason`
- the originating rule path, line, and column

Typical fixes:

- remove the implementation body from the contract member;
- replace mutable setters with `get` / `init`;
- move stateful helpers or concrete implementations out of the contract layer;
- broaden the policy only when that contract shape is intentional.

**Focused example projects:**

- [`Example.Arch013.ContractPurity`](../../Examples/Diagnostics/Example.Arch013.ContractPurity) - property setters are rejected by `allowedPropertyAccessors="Get"`.
- [`Example.Arch013.ContractPurity.MethodBodyNotAllowed`](../../Examples/Diagnostics/Example.Arch013.ContractPurity.MethodBodyNotAllowed) - default interface method bodies are rejected by `allowMethodBodies="false"`.
