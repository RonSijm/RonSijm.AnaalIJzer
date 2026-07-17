### `<Allowed>` type policy

`<Allowed>` is a whitelist for dependency types. A dependency assigned to a configured layer must match at least one `<Class>` or `<Namespace>` matcher in every applicable allow-list; otherwise the analyzer reports **ARCH003**.

At the root, the allow-list applies to every dependency that belongs to a configured layer:

```xml
<Allowed>
  <Class startsWith="Create" />
  <Class startsWith="Cancel" />
</Allowed>
```

This is useful when an architecture permits only a small vocabulary, such as command verbs. Matchers within one scope are alternatives, so the example accepts both `CreateOrderCommand` and `CancelOrderCommand` but rejects `ProcessOrderCommand`.

```csharp
public class CreateOrderCommand { }
public class CancelOrderCommand { }

// ARCH003: Process is not in the approved global verb list.
public class ProcessOrderCommand { }
public class WorkflowService(ProcessOrderCommand command) { }
```

The policy is checked when a layered type uses the dependency. It does not report on an otherwise unused type declaration.

**Example project:** [`Example.AllowedTypes`](../../Examples/Features/Example.AllowedTypes)

#### Layer-scoped type policies

Place `<Allowed>` or `<Forbidden>` inside a `<Layer>` to restrict the policy to dependencies classified into that layer and its descendants:

```xml
<Layer name="Command">
  <Class endsWith="Command" />
  <Allowed>
    <Class startsWith="Create" />
    <Class startsWith="Cancel" />
  </Allowed>
</Layer>

<Layer name="Query">
  <Class endsWith="Query" />
  <Forbidden>
    <Class startsWith="Delete" />
  </Forbidden>
</Layer>
```

`ProcessOrderCommand` fails the `Command` allow-list, while `DeleteOrderQuery` matches the `Query` block-list. A type named `DeleteOrderAuditRecord` in an `Audit` layer is unaffected: the `Query` policy does not leak into sibling layers.

Nested policies are cumulative. A dependency in `Ordering/Command` must satisfy allow-lists declared on both `Ordering` and `Ordering/Command`. Any matching forbidden rule denies the dependency, even when an allow-list also matches it.

**Example project:** [`Example.ScopedTypePolicies`](../../Examples/Features/Example.ScopedTypePolicies)
