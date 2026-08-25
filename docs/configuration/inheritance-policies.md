### Inheritance policies

`<InheritancePolicy>` requires declarations in a layer to inherit a specific base type or implement specific interfaces. It is opt-in and separate from dependency permission, visibility, and contract purity.

Use it when a layer has a semantic base contract that every declaration must follow:

```xml
<Layer name="PersistenceEntities">
  <Namespace startsWith="Shop.Persistence" />

  <InheritancePolicy
    typeKinds="Class, Record"
    requiredBaseTypes="Entity, AggregateRoot"
    requiredInterfaces="IAuditedEntity"
    description="Persistence entities inherit the shared entity base and auditing contract." />
</Layer>
```

#### Type-kind values

`typeKinds` is required. Tokens are case-insensitive.

| Value | Meaning |
|---|---|
| `Class` | Class declarations, excluding records |
| `Interface` | Interface declarations |
| `Struct` | Struct declarations, excluding record structs |
| `Record` | Record class declarations |
| `RecordStruct` | Record struct declarations |
| `Enum` | Enum declarations |
| `Delegate` | Delegate declarations |

#### Required contracts

At least one of `requiredBaseTypes` and `requiredInterfaces` is required.

- `requiredBaseTypes` passes when the declaration inherits **any** listed base type.
- `requiredInterfaces` passes when the declaration implements **all** listed interfaces.

Both attributes accept comma-separated simple or fully qualified type names.

```xml
<InheritancePolicy typeKinds="Class" requiredBaseTypes="Entity" />
<InheritancePolicy typeKinds="Class, Record" requiredInterfaces="IAuditedEntity, ISoftDelete" />
<InheritancePolicy typeKinds="Class" requiredBaseTypes="AggregateRoot" requiredInterfaces="IAuditedEntity" />
```

#### Nested layers

Inheritance policies apply to the owning layer and all descendants. Parent and child policies are cumulative:

```xml
<Layer name="Domain">
  <Namespace startsWith="Shop.Domain" />
  <InheritancePolicy typeKinds="Class" requiredInterfaces="IDomainType" />

  <Layer name="Entities">
    <Class endsWith="Entity" />
    <InheritancePolicy typeKinds="Class" requiredBaseTypes="Entity" />
  </Layer>
</Layer>
```

The child policy cannot override an outer denial. The first failure is reported from outermost to innermost.

#### What this rule does not mean

- Inheritance policies do not grant or deny dependency edges. That is still controlled by `<AllowedDependency>` and `<BlockedDependency>`.
- Inheritance policies do not decide whether a declaration may be `public` or `internal`. That is a visibility-policy concern (`ARCH012`).
- Inheritance policies do not replace contract purity. A type can inherit the right base class and still violate `<ContractPolicy>`.
- Inheritance policies check declarations that already exist. They do not classify a type into a layer by themselves; the layer matchers still do that.

Arse includes inheritance-policy findings in `inspect`, `report`, generated documentation, and code evidence. The standalone WPF editor and Visual Studio graph inspector expose the same settings at layer scope.

**Example project:** [`Example.Arch019.InheritancePolicy`](../../Examples/Diagnostics/Example.Arch019.InheritancePolicy)
