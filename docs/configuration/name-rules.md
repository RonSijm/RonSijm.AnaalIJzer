### `<NameRules>`

`NameRules` are layer-scoped semantic-name policies. They do not create layer dependencies. They can check either a named value moving into a differently named target or a declaration identifier that disagrees with its own semantic type.

Use this when primitive values are still necessary, but you want some of the protection people often get from "honest types". To the compiler one `int` is exactly as meaningful as any other `int`, which is why swapped id arguments pass review so comfortably and reappear later as a production incident:

```xml
<Layer name="Application">
  <Class endsWith="Service" />

  <NameRules>
    <RequireMatchingNames>
      <Name endsWith="Id" />
      <Allow from="legacyCustomerId" to="customerId" allowedSites="Constructor" />
    </RequireMatchingNames>
  </NameRules>
</Layer>
```

`RequireMatchingNames` above says:

| Element | Meaning |
|---|---|
| `<Name endsWith="Id" />` | Check source or target names ending with `Id`. |
| `<Allow from="legacyCustomerId" to="customerId" />` | This intentional rename is allowed. |
| `allowedSites="Constructor"` | The rename is allowed only when calling a constructor. |

The analyzer normalizes names before comparing them. For example, `customerId` and `Customer.Id` are treated as the same meaning. `fruitId` and `animalId` are not.

```csharp
// Valid: customerId normalizes to Customer.Id.
customer.Id = customerId;

// ARCH_NAME_008: animalId does not mean Customer.Id.
customer.Id = animalId;

// ARCH_NAME_008: arguments are swapped.
Log(animalId, fruitId);

void Log(int fruitId, int animalId) { }
```

#### Matchers

`Name`, `Source`, and `Target` use the same matcher attributes and AND/OR behavior as layer `<Class>` matchers:

```xml
<RequireMatchingNames>
  <Name startsWith="customer" endsWith="Id" />
</RequireMatchingNames>

<RequireMatchingNames>
  <Source endsWith="RowId" />
  <Target endsWith="Id" />
  <Allow>
    <Source exactName="customerRowId" />
    <Target exactName="Customer.Id" />
  </Allow>
</RequireMatchingNames>
```

Multiple attributes on one matcher are combined with AND semantics. Multiple matcher elements are alternatives.

#### Sites

`RequireMatchingNames` and nested `Allow` mappings support the same `allowedSites` and `blockedSites` attributes as dependency edges. The first implementation reports value-name movements at these sites:

| Site | Example |
|---|---|
| `Constructor` | `new Customer(legacyCustomerId)` compared with constructor parameter `customerId` |
| `Method` | `Save(animalId)` compared with method parameter `fruitId` |
| `MethodReturn` | `return animalId;` compared with the containing method name |
| `Field` | `_fruitId = animalId` or field initializer assignment |
| `Property` | `customer.Id = animalId` or property initializer assignment |
| `Local` | `var fruitId = animalId` or `fruitId = animalId` |

Other site names remain valid in filters because the site vocabulary is shared across the analyzer, but NameRules only produce diagnostics for value movements that have both a source name and a target name.

#### Direct language forms

`RequireMatchingNames` checks direct value movements by default. That includes ordinary and compound assignments, component-wise tuple/deconstruction assignments, object-construction arguments, named and optional arguments, `in` arguments, `out` values flowing back to the caller, direct returns, and expression-bodied methods, properties, indexers, and local functions. Parentheses, conversions, `as`, null-forgiving operators, conditional branches, coalesce expressions, and tuple expressions are unwrapped into their direct named sources.

Anonymous-lambda returns intentionally have no named return target, so the rule does not compare them with the containing method. Calls and assignments *inside* the lambda are still analysed at their own sites. This prevents a lambda from being accidentally reported as though it returned from its outer method.

**Direct-form examples:** [`Example.NameRules`](../../Examples/Features/Example.NameRules) and [`Example.NameRuleLanguageForms`](../../Examples/Features/Example.NameRuleLanguageForms).

#### `valueTracking`: direct versus local provenance

`valueTracking` belongs only on `<RequireMatchingNames>`:

| Value | Default | What it compares |
|---|---:|---|
| `Direct` | Yes | The value written directly at the current site. It does not follow local aliases. |
| `IntraProcedural` | No | The direct value plus an unambiguous local alias within the same method, accessor, constructor, local function, or lambda body. |

Use `Direct` for the least surprising and fastest rule. Enable `IntraProcedural` only when a neutral local name can conceal a meaningful value name before it reaches another meaningful target:

```xml
<RequireMatchingNames valueTracking="IntraProcedural">
  <Source endsWith="Id" />
  <Target endsWith="Id" />
</RequireMatchingNames>
```

```csharp
var pending = customerId;
Save(pending); // ARCH_NAME_008 when Save accepts orderId.
```

Method-like bodies use Roslyn control-flow graphs, so a branch join keeps provenance only when every path agrees. Roslyn does not expose a standalone control-flow graph root for a lambda body, so lambda bodies use a conservative ordered scan and discard local provenance before a conditional, loop, switch, or `try` block. A captured parameter can still be the direct source inside a lambda, but a local alias never crosses a callback boundary. Tracking intentionally stops at method calls, virtual dispatch, collections, delegate invocation, reflection, and method boundaries. That is a bounded local-provenance check, not a whole-program taint-analysis promise.

**Tracking example:** [`Example.NameRuleIntraProceduralTracking`](../../Examples/Features/Example.NameRuleIntraProceduralTracking).

#### Declaration names and semantic types

`RequireDeclarationNameMatchesType` checks the declaration itself. This is useful when serializers, model binders, dependency injection, or humans rely on an identifier to describe a strongly typed value:

```xml
<Layer name="AspEndpoints">
  <Class endsWith="Endpoint" />
  <NameRules>
    <RequireDeclarationNameMatchesType allowedSites="Method, Property">
      <Type implements="IHonestType" />
    </RequireDeclarationNameMatchesType>
  </NameRules>
</Layer>
```

```csharp
public void GetPatient(PatientId patientId) { } // Allowed
public void GetPatient(DoctorId patientId) { }  // ARCH_NAME_008

public PatientId PatientId { get; set; } // Allowed
public DoctorId PatientId { get; set; }  // ARCH_NAME_008
```

`Type` selects semantic declared types. `Name` optionally selects declaration identifiers. Both use the same conjunctive matcher attributes as `Class`, and multiple sibling matchers are alternatives:

```xml
<RequireDeclarationNameMatchesType allowedSites="Method">
  <Type implements="IHonestType" endsWith="Id" />
  <Name endsWith="Id" />
  <Allow from="LegacyPatientIdentifier" to="patientId" />
</RequireDeclarationNameMatchesType>
```

The supported declaration sites are:

| Site | Declaration compared with its semantic type |
|---|---|
| `Constructor` | Constructor or primary-constructor parameter |
| `Method` | Ordinary method parameter |
| `MethodReturn` | Method name and return type |
| `Field` | Each declared field variable |
| `Property` | Property name and property type |
| `Local` | Explicit or `var` local variable |

These two rules answer different questions:

| Code | Responsible rule |
|---|---|
| `DoctorId patientId` | `RequireDeclarationNameMatchesType`: the declaration name disagrees with its type |
| `PatientId patientId = doctorId` | `RequireMatchingNames`: a differently named value moves into the declaration |
| `DoctorId GetPatientId()` | `RequireDeclarationNameMatchesType` at `MethodReturn` |
| `return doctorId;` from `GetPatientId` | `RequireMatchingNames` at `MethodReturn` |

Declaration rules use the semantic type, so aliases and `var` are resolved by Roslyn. Nullable value types are unwrapped. Arrays, collections, `Task<T>`, and arbitrary generic wrappers are not implicitly projected to an inner type.

**Examples:** [`Example.DeclarationNameMatchesType`](../../Examples/Features/Example.DeclarationNameMatchesType) covers all six declaration sites. [`Example.HonestTypeEndpointNames`](../../Examples/Scenarios/Example.HonestTypeEndpointNames) shows the convention-based endpoint binding use case.
