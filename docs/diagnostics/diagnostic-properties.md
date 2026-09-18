### Diagnostic properties

Every dependency diagnostic (ARCH_DEP_001, ARCH_DEP_004, ARCH_DEP_005), name-rule diagnostic (ARCH_NAME_008), and API-surface diagnostic (ARCH_API_001 and ARCH_API_010) carries a `Site` property in `Diagnostic.Properties` indicating where the issue was found. This lets code-fix providers, custom reporters and CI dashboards filter or group by dependency style without re-parsing the source - which beats a dashboard built on regexes over diagnostic messages that breaks the day the wording improves.

ARCH_VIS_001 describes declarations rather than dependency sites. It exposes `DeclarationTarget`, `DeclaredAccessibility`, and `DeclaredSymbolName` alongside the caller layer and rule-origin properties.

ARCH_INH_001 also describes declarations rather than dependency sites. It exposes `DeclaredSymbolName` and `InheritanceViolationKind` alongside the caller layer and rule-origin properties.

ARCH_RET_001 exposes `Site` as `MethodReturn`, together with `DeclaredSymbolName`, `ReturnValueRuleTarget`, `ReturnValueRule`, and `ReturnValueRuleMode`. `ReturnValueRuleMode` is `Forbidden` for a matching direct forbidden matcher and `Allowed` when the returned expression did not match an `<AllowedReturn>` shape allow-list.

ARCH_OPER_001 exposes `Site`, `OperationKind`, `OperationDisplayName`, and `OperationPolicyRule` so reports can distinguish, for example, a forbidden `DateTime.UtcNow` property read from a forbidden `Task.Wait()` invocation.

`ARCH_OPER_002`, `ARCH_OPER_011`, and `ARCH_OPER_012` expose `Site`, `DeclaredSymbolName`, `OperationKind`, `OperationDisplayName`, `OperationPolicyRule`, `BehavioralOperationViolationKind`, and `BehavioralOperationOrdering`. A missing required operation uses the owning declaration location and its ordinary declaration site; a selected failing operation uses that operation's source site.

ARCH_ASSM_001 describes emitted assembly metadata rather than a dependency site. It exposes `AssemblyAttributeTypeName` and `AssemblyAttributePolicyRule` alongside the normal caller, rule-origin, and configuration-location properties. SDK-generated attributes can have no source span, because the project SDK created the final attribute.

ARCH_NS_007 exposes `CallerNamespace`, `DependencyNamespace`, `NamespaceHierarchyRoot`, `NamespaceHierarchyRelation`, `NamespaceHierarchyRuleXmlPath`, `NamespaceHierarchyRuleXmlLine`, and `NamespaceHierarchyRuleXmlCol`. Its `Site` identifies the resolved source dependency that crossed the configured namespace-ownership boundary.

ARCH_API_001 additionally exposes `ApiMemberName`, identifying the externally visible declaration that published the dependency type.

ARCH_API_010 adds `ExposureRootMember`, `ExposurePath`, `ExposureDepth`, `NestedMemberName`, and `NestedMemberContainingType`. Its `Site` identifies the nested public member that exposed the forbidden type rather than the root signature site.

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
