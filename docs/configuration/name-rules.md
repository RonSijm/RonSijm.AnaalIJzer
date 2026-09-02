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

// ARCH008: animalId does not mean Customer.Id.
customer.Id = animalId;

// ARCH008: arguments are swapped.
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

**Example project:** [`Example.NameRules`](../../Examples/Features/Example.NameRules)

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
public void GetPatient(DoctorId patientId) { }  // ARCH008

public PatientId PatientId { get; set; } // Allowed
public DoctorId PatientId { get; set; }  // ARCH008
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
