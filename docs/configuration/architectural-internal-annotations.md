### Architectural-internal annotations

Sometimes `internal` is exactly what you want. Sometimes a framework has other ideas.

An entity projection may need to stay `public` because Entity Framework, a serializer, or reflection needs to construct it. That CLR requirement does not mean the rest of the application should start passing the projection around as a public contract.

Install the small annotations package in the project that owns such declarations:

```shell
dotnet add package RonSijm.IJzer.Annotations
```

The package has no analyzer, Roslyn, or runtime dependencies. The normal `RonSijm.IJzer` analyzer recognizes its attributes by their full metadata names.

#### Pick the boundary you mean

| Attribute | Who may reference the declaration? | Needs layers? |
|---|---|---|
| `[ClassInternal]` | The declaring or annotated type and its nested type family | No |
| `[AssemblyInternal]` | Code in the declaring assembly | No |
| `[LayerInternal]` | Code in the declaration's deepest configured layer and descendant layers | Yes |

`ClassInternal` is useful for a reflection-only public class without making you invent a one-class layer:

```csharp
using RonSijm.IJzer.Annotations;

[ClassInternal]
public sealed class ReflectionProjection
{
    public static ReflectionProjection Create() => new();
}
```

Code inside `ReflectionProjection`, including nested types, may use it. Another top-level class may not directly name or construct it.

For the entity-projection case, a layer boundary is usually the better fit:

```csharp
[LayerInternal]
public sealed record PatientEntityProjection(int PatientId, string DisplayName);
```

```xml
<Layer name="Persistence">
  <Class endsWith="EntityProjection" />
  <Class endsWith="Repository" />
</Layer>
```

A repository in `Persistence` may use the projection. An application service may not, even when an ordinary `<AllowedDependency from="Application" to="Persistence" />` edge exists. The edge permits the layer dependency; `[LayerInternal]` narrows this particular declaration.

#### Public signatures count too

The restriction is checked at both ends:

- code outside the boundary may not reference the annotated declaration;
- public API inside the boundary may not leak it to outside callers.

This is rejected:

```csharp
[AssemblyInternal]
public sealed class EntityProjection { }

public sealed class ProjectionFacade
{
    public EntityProjection Leak() => new();
}
```

Protect the facade with an equivalent or narrower annotation, or return a proper public contract instead.

#### What the attributes do not do

- They do not rewrite `public` to `internal` in IL.
- They do not change reflection or framework behavior.
- They do not replace ordinary C# accessibility when `internal` or `private` already works.
- `[LayerInternal]` fails closed when its declaration is not classified into a layer.

There is deliberately no automatic code fix. Removing a reference, changing a return contract, or widening a boundary are different design decisions, and the light bulb cannot honestly pick one for you.

**Example project:** [`Example.Arch_VIS_007.ArchitecturalInternal`](../../Examples/Diagnostics/VIS/Example.Arch_VIS_007.ArchitecturalInternal)
