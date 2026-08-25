### Visibility policies

`<VisibilityPolicy>` restricts the declared accessibility of types and members owned by a layer. It is opt-in and does not create or block a dependency edge.

Use an allowlist when only a small set is acceptable:

```xml
<Layer name="RepositoryQuerySurface">
  <Class endsWith="Queryable" />

  <VisibilityPolicy
    targets="Type"
    allowedAccessibilities="Internal, File"
    description="Repository query surfaces are implementation details." />
</Layer>
```

Use a blocklist when most accessibilities are acceptable:

```xml
<VisibilityPolicy
  targets="Field, Property"
  blockedAccessibilities="Public, Protected, ProtectedInternal" />
```

Exactly one of `allowedAccessibilities` and `blockedAccessibilities` is required.

#### Declaration targets

`targets` is a required, comma-separated list. Tokens are case-insensitive.

| Target | Declaration |
|---|---|
| `Type` | Top-level class, interface, struct, record, enum, or delegate |
| `NestedType` | A type declared inside another type |
| `Constructor` | Instance or static constructor |
| `Method` | Ordinary method, including an explicit interface implementation |
| `Property` | Property or indexer |
| `Field` | Field or field-like enum member |
| `Event` | Field-like or explicit event |
| `Operator` | User-defined operator |
| `Conversion` | Implicit or explicit conversion operator |

Implicit compiler-generated declarations are ignored. Partial symbols are evaluated once.

#### Accessibility values

Accessibility lists support:

| Value | C# form |
|---|---|
| `Public` | `public` |
| `Internal` | `internal` or the default for a top-level type |
| `Protected` | `protected` |
| `ProtectedInternal` | `protected internal` |
| `PrivateProtected` | `private protected` |
| `Private` | `private` or the default for a class member |
| `File` | `file` type |

The analyzer uses Roslyn symbols, not modifier text. Interface members therefore have their semantic public accessibility, and explicit interface implementations have their semantic private accessibility.

#### Nested layers

Policies apply to the owning layer and all descendants. Parent and child policies are cumulative, and every applicable policy must pass:

```xml
<Layer name="Application">
  <Assembly exactName="Restaurant.Application" />
  <VisibilityPolicy targets="Field" blockedAccessibilities="Public" />

  <Layer name="Contracts">
    <Class endsWith="Contract" />
    <VisibilityPolicy targets="Type" allowedAccessibilities="Public" />
  </Layer>
</Layer>
```

The child policy cannot override a parent failure. The first failing policy is reported from outermost to innermost.

#### What this rule does not mean

- Visibility policies check a declaration's own accessibility. A public nested class inside an internal parent is still declared `Public`.
- Whether a declaration is effectively visible outside all its containing types is exposed to documentation and editor tooling for context, but it does not change `ARCH012`.
- Whether a public signature exposes a forbidden layer is a separate API-surface concern (`ARCH009`).
- Whether an interface or contract contains an allowed kind of member is a separate contract-purity concern (`ARCH013`).

Arse includes visibility findings in `inspect`, `report`, generated documentation, and code evidence. The standalone WPF editor and Visual Studio graph inspector provide checkable target/accessibility controls and autosave the same `.anl` or inline metadata source.

**Example project:** [`Example.Arch012.VisibilityPolicy`](../../Examples/Diagnostics/Example.Arch012.VisibilityPolicy)
