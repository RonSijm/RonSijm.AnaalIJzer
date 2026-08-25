## API surface policies

An `<AllowedDependency>` answers whether code may **use** another layer. An `<ApiSurface>` answers a different question: whether an externally visible declaration may **expose** that layer to its callers.

This distinction is useful for repository-owned fluent query surfaces. An application service may use a `LollyQueryable` internally, but its public API should return a stable `LollyProjection` contract:

```xml
<Layer name="Application">
  <Class endsWith="Service" />

  <ApiSurface description="Public application APIs expose contracts only.">
    <AllowedLayer path="/Contracts" />
    <BlockedLayer path="/RepositoryQuerySurface" />
  </ApiSurface>
</Layer>

<Layer name="Contracts">
  <Class endsWith="Projection" />
</Layer>

<Layer name="RepositoryQuerySurface">
  <Class endsWith="Queryable" />
</Layer>

<!-- Internal use remains a separate dependency decision. -->
<AllowedDependency from="Application" to="RepositoryQuerySurface" />
```

```csharp
public class CandyOrderingService
{
    // Allowed: the public result is a contract.
    public LollyProjection OrderProjectedLolly() => null!;

    // Allowed: a private implementation detail is not external API.
    private LollyQueryable BuildQuery() => null!;

    // ARCH009: a repository-owned query surface escapes through public API.
    public LollyQueryable OrderRawLolly() => null!;
}
```

### Evaluation rules

- The policy applies to its layer and all descendant layers.
- Parent and child policies are cumulative; a child cannot override a parent denial.
- A matching `<BlockedLayer>` wins over an `<AllowedLayer>`.
- If one or more `<AllowedLayer>` entries apply at the current site, the exposed recognized type must match one of them.
- A parent layer path selects its complete subtree.
- By default, unclassified framework and third-party types are ignored.
- Set `requireRecognizedTypes="true"` to reject unclassified exposed types too.
- Only externally visible declarations are checked: `public`, `protected`, and `protected internal`, through an externally visible containing-type chain.

### API sites

| Site | Exposed declaration |
|---|---|
| `Constructor` | Constructor parameter |
| `Method` | Method or delegate parameter |
| `MethodReturn` | Method or delegate return type |
| `Property` | Property, indexer type, or indexer parameter |
| `Field` | Field or event type |
| `Inheritance` | Base class |
| `InterfaceImplementation` | Implemented interface |
| `GenericArgument` | Generic arguments and nested signature parts |
| `Attribute` | Attribute type on an externally visible declaration |

`allowedSites` makes an API layer rule apply only at the listed sites. `blockedSites` makes it apply everywhere except the listed sites:

```xml
<AllowedLayer path="/Contracts" allowedSites="Method, MethodReturn, Property" />
<BlockedLayer path="/RepositoryQuerySurface" blockedSites="Method" />
```

Locals, object creation, generic invocation, and static member access are implementation behavior rather than API declarations, so `<ApiSurface>` does not inspect them.

To inspect the public object graph behind an allowed signature type, enable [`TransitiveExposure`](transitive-api-exposure.md). Direct violations remain ARCH009; hidden violations reached through a permitted contract report ARCH014.

**Example project:** [`Example.Arch009.ApiSurfaceLeakage`](../../Examples/Diagnostics/Example.Arch009.ApiSurfaceLeakage)
