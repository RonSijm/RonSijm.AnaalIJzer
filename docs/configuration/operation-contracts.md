## Operation contracts

`<Operations>` describes a named source-level operation only when a team explicitly writes it down. It connects a selected owner method to zero or more selected entry points and, optionally, to request and response type shapes. It does not infer HTTP routes, ASP.NET controllers, queue handlers, scheduled jobs, or business meaning from names.

Use it when a few important paths deserve a stronger, named rule than ordinary dependency direction. A pizza order is a useful small example: a waiter may receive `PlacePizzaOrderRequest`, but the kitchen owns the operation. A selected waiter method must call that kitchen method directly, and both methods may be required to use the same request and response types.

```xml
<Operations description="Important restaurant operations are named explicitly.">
  <Operation name="PlacePizzaOrder"
             allowedOwnerLayers="Application"
             allowedEntryPointLayers="Controller"
             description="A waiter submits one order to the kitchen.">
    <Owner>
      <DeclarationMatcher>
        <ContainingType endsWith="Kitchen" />
        <Member exactName="PlacePizzaOrder" memberKind="Method" />
      </DeclarationMatcher>
    </Owner>
    <Request><Class exactName="PlacePizzaOrderRequest" /></Request>
    <Response><Class exactName="PlacePizzaOrderResponse" /></Response>
    <EntryPoint>
      <DeclarationMatcher>
        <ContainingType endsWith="Controller" />
        <Member exactName="PlacePizzaOrder" memberKind="Method" />
      </DeclarationMatcher>
    </EntryPoint>
  </Operation>
</Operations>
```

### Meaning

| Element or attribute | Meaning |
| --- | --- |
| `<Operation name="...">` | A human-selected identifier. It is not inferred from code. Names are unique across loaded configuration files. |
| `<Owner>` | Exactly one required method declaration selector. The workspace host checks that it resolves to exactly one source method in the inspected project or solution. |
| `<EntryPoint>` | Zero or more method selectors. Each matching method must directly invoke the configured owner. |
| `<Request>` | Optional `<Class>` matcher. When present, the owner and selected entry points must each accept a matching parameter. |
| `<Response>` | Optional `<Class>` matcher. When present, the owner and selected entry points must return a matching direct return type. `Task<T>` is not unwrapped implicitly. |
| `allowedOwnerLayers` | Optional comma-separated layer paths for the owner. Use `Application` for a root layer and `/Ordering/Application` for a nested layer. |
| `allowedEntryPointLayers` | Optional comma-separated layer paths for entry points. |

`ContainingType` and `Member` inside a `DeclarationMatcher` use the shared semantic matcher vocabulary and are combined with AND semantics. The owner and entry-point `Member` must use `memberKind="Method"`. `Request` and `Response` use the normal `<Class>` matcher vocabulary, including combined attributes and structural declaration matchers.

### What is checked where

The compiler analyzer reports `ARCH023` for local facts:

- a selected owner or entry point is outside its configured host layer;
- a selected owner or entry point lacks the configured request parameter;
- a selected owner or entry point returns the wrong direct response type;
- a selected entry point does not directly invoke the selected owner.

The workspace-backed Arse commands (`arse inspect` and `arse report`) report a separate finding when an operation has no matching owner or more than one matching owner across the inspected scope. The graph editors preserve and edit the source contract; that cardinality fact cannot be proven by one project's compiler analyzer invocation.

The check deliberately does not follow helpers, delegates, asynchronous continuations, reflection, or method return values. A direct call is a clear source-level contract; anything broader needs a separate explicit policy rather than an optimistic guess.

### Tool support

- **Arse:** `inspect` and `report` include operation-owner cardinality findings for a project or solution. `documentation` renders the manifest in XML order.
- **WPF graph editor and Visual Studio graph host:** the root inspector can add, edit, or remove an `<Operations>` container. It is presented as a source-contract editor, not a dependency graph edge.
- **Visual Studio editor:** when Sites Diagnostics are enabled, local `ARCH023` violations appear as method-site indicators with QuickInfo.
- **Code fixes:** none. Connecting an entry point to a workflow, or deciding how to reshape a request/response contract, is a domain decision.

**Focused example:** [`Example.Arch023.OperationContract`](../../Examples/Diagnostics/Example.Arch023.OperationContract)
