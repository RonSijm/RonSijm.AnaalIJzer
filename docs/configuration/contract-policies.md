### Contract purity

`<ContractPolicy>` restricts which declaration shapes are acceptable for contract types owned by a layer. It is opt-in and separate from dependency permission, visibility, and API exposure.

Use it when a layer should stay abstract and message-like:

```xml
<Layer name="Contracts">
  <Class endsWith="Contract" typeKind="Interface" />

  <ContractPolicy
    allowedTypeKinds="Interface"
    allowedMemberKinds="Method, Property"
    allowedPropertyAccessors="Get, Init"
    allowMethodBodies="false"
    allowStaticMembers="false"
    allowNestedTypes="false"
    description="Contracts stay abstract and expose immutable state only." />
</Layer>
```

#### Type-kind values

`allowedTypeKinds` is required. Tokens are case-insensitive.

| Value | Meaning |
|---|---|
| `Class` | Class declarations, excluding records |
| `Interface` | Interface declarations |
| `Struct` | Struct declarations, excluding record structs |
| `Record` | Record class declarations |
| `RecordStruct` | Record struct declarations |
| `Enum` | Enum declarations |
| `Delegate` | Delegate declarations |

#### Member-kind values

`allowedMemberKinds` is required. Tokens are case-insensitive.

| Value | Declaration |
|---|---|
| `Constructor` | Instance or static constructor |
| `Method` | Ordinary method |
| `Property` | Property or indexer |
| `Event` | Event |
| `Field` | Field |
| `Operator` | User-defined operator |
| `Conversion` | Implicit or explicit conversion operator |

#### Property accessors

`allowedPropertyAccessors` is optional. When omitted, accessors are not restricted.

| Value | Meaning |
|---|---|
| `Get` | Getter accessor |
| `Set` | Setter accessor |
| `Init` | Init accessor |

#### Boolean settings

All booleans default to `false` when omitted.

| Attribute | Meaning when `false` |
|---|---|
| `allowMethodBodies` | Methods, accessors, and default interface members must stay body-free |
| `allowStaticMembers` | Source-declared static members are rejected |
| `allowNestedTypes` | Source-declared nested types are rejected |

#### Nested layers

Contract policies apply to the owning layer and all descendants. Parent and child policies are cumulative:

```xml
<Layer name="Application">
  <Assembly exactName="Restaurant.Application" />
  <ContractPolicy
    allowedTypeKinds="Interface, Record"
    allowedMemberKinds="Method, Property" />

  <Layer name="Contracts">
    <Class endsWith="Contract" />
    <ContractPolicy
      allowedTypeKinds="Interface"
      allowedMemberKinds="Method, Property"
      allowedPropertyAccessors="Get" />
  </Layer>
</Layer>
```

The child policy cannot override an outer denial. The first failure is reported from outermost to innermost.

#### What this rule does not mean

- Contract purity does not grant or deny dependency edges. That is still controlled by `<AllowedDependency>` and `<BlockedDependency>`.
- Contract purity does not decide whether a declaration may be `public` or `internal`. That is a visibility-policy concern (`ARCH012`).
- Contract purity does not decide whether a public signature leaks a forbidden layer. That is an API-surface concern (`ARCH009` / `ARCH014`).
- Contract purity is not inferred from a layer name such as `Contracts`; it only runs when `<ContractPolicy>` is present.

Arse includes contract-purity findings in `inspect`, `report`, generated documentation, and code evidence. The standalone WPF editor and Visual Studio graph inspector expose the same settings as token checklists and booleans.

**Focused example projects:**

- [`Example.Arch013.ContractPurity`](../../Examples/Diagnostics/Example.Arch013.ContractPurity) - getter-only contract properties; setters trigger `ARCH013`.
- [`Example.Arch013.ContractPurity.MethodBodyNotAllowed`](../../Examples/Diagnostics/Example.Arch013.ContractPurity.MethodBodyNotAllowed) - contract methods stay signature-only; default interface method bodies trigger `ARCH013`.
