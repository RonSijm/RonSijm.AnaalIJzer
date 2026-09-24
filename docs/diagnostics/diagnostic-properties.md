### Diagnostic properties

Dependency diagnostics (`ARCH_DEP_001`, `ARCH_DEP_004`, and `ARCH_DEP_005`), `ARCH_NAME_008`, and the API-surface diagnostics carry a `Site` property in `Diagnostic.Properties`. Code-fix providers, reporters, and CI dashboards can group findings without parsing message text, which beats a regex dashboard that breaks the day the wording improves.

The policy families add their own properties:

- `ARCH_VIS_001`
  - `DeclarationTarget`, `DeclaredAccessibility`, and `DeclaredSymbolName`;
- `ARCH_INH_001`
  - `DeclaredSymbolName` and `InheritanceViolationKind`;
- `ARCH_RET_001`
  - `Site=MethodReturn`, `DeclaredSymbolName`, `ReturnValueRuleTarget`, `ReturnValueRule`, and `ReturnValueRuleMode`;
  - `ReturnValueRuleMode` is `Forbidden` for a matching forbidden expression and `Allowed` when no `<AllowedReturn>` shape matched;
- `ARCH_OPER_001`
  - `Site`, `OperationKind`, `OperationDisplayName`, and `OperationPolicyRule`;
- `ARCH_OPER_002`, `ARCH_OPER_011`, and `ARCH_OPER_012`
  - `Site`, `DeclaredSymbolName`, `OperationKind`, `OperationDisplayName`, `OperationPolicyRule`, `BehavioralOperationViolationKind`, and `BehavioralOperationOrdering`;
  - a missing operation points at its owning declaration, while a selected failing operation points at the operation itself;
- `ARCH_ASSM_001`
  - `AssemblyAttributeTypeName` and `AssemblyAttributePolicyRule`, plus the normal caller and rule-origin properties;
  - an SDK-generated attribute may have no source span because the SDK emitted it;
- `ARCH_NS_007`
  - `CallerNamespace`, `DependencyNamespace`, `NamespaceHierarchyRoot`, `NamespaceHierarchyRelation`, and the rule XML path/line/column properties;
- `ARCH_API_001`
  - `ApiMemberName`, identifying the declaration that published the dependency type;
- `ARCH_API_010`
  - `ExposureRootMember`, `ExposurePath`, `ExposureDepth`, `NestedMemberName`, and `NestedMemberContainingType`;
  - its `Site` identifies the nested public member that exposed the forbidden type.

| `Site` value        | Where the dependency was introduced                                        |
|---------------------|----------------------------------------------------------------------------|
| `Constructor`       | Constructor parameter (including primary constructors)                      |
| `Method`            | Non-constructor method parameter                                            |
| `MethodReturn`      | Non-constructor method return type                                          |
| `Field`             | Field declaration                                                           |
| `Property`          | Property declaration                                                        |
| `Local`             | Local variable declaration                                                  |
| `New`               | `new T(...)` or target-typed `new()` expression                             |
| `GenericArgument`   | Generic type argument of an outer type (`Lazy<T>`, `IEnumerable<T>`, …)     |
| `GenericInvocation` | Generic method invocation (service-locator style: `services.GetService<T>()`) |
| `Inheritance`       | Base class inheritance or interface-to-interface inheritance                 |
| `InterfaceImplementation` | Class, record, or struct implements an interface                       |
| `Attribute`         | Attribute used on a type or one of its members                              |
| `StaticMember`      | Static method, property, field, event, or reduced extension-method access   |

**Example project:** [`Example.Arch_DEP_001.NonConstructorInjection`](../../Examples/Diagnostics/DEP/Example.Arch_DEP_001.NonConstructorInjection)

**Rule:** Dependencies introduced outside the constructor are still dependencies. Fields, properties, method signatures, local variables, inheritance, interface implementation, attributes, static member access, `new` expressions and generic service-locator invocations are all checked against the configured layer edges. Classes, records, structs, and interfaces can all act as callers.

**Type-kind example:** [`Example.NonClassCallers`](../../Examples/Features/Example.NonClassCallers)

```mermaid
flowchart LR
    Customer --> Waiter --> Chef
    Customer -. "bad: hidden Chef dependency" .-> Chef
```

```xml
<AllowedDependency from="Customer" to="Waiter" />
<AllowedDependency from="Waiter" to="Chef" />
<!-- Customer -> Chef: intentionally omitted -->
```

```csharp
// ARCH_DEP_001: field dependency
public class FieldDependencyCustomer
{
    private readonly IChef _chef = null!;
}

// ARCH_DEP_001: property dependency
public class PropertyDependencyCustomer
{
    public IChef Chef { get; set; } = null!;
}

// ARCH_DEP_001: method parameter
public class MethodDependencyCustomer
{
    public void OrderFrom(IChef chef) { }
}

// ARCH_DEP_001: method return type
public class MethodReturnCustomer
{
    public IChef FindChef() => null!;
}

// ARCH_DEP_001: creating a Chef directly
public class NewingCustomer
{
    public void Run() => _ = new DirectChef();
}

// ARCH_DEP_001: a hidden lookup still bypasses the Waiter.
public class ServiceLocatorCustomer
{
    public void Run(IServiceProvider services)
        => _ = services.GetRequiredService<IChef>();
}
```

---
