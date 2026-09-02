### ARCH002 - Unrecognized dependency

Reported when a layered type uses a dependency that does not belong to any configured layer and root-level or caller-layer `requireRecognizedDependencies` includes the current site.

**Example output:**
```
error ARCH002: 'ExperimentalChef' (layer Chef) depends on 'MysteryBox'
  which is not assigned to any architectural layer
```

#### Choose recognition sites deliberately

`Constructor` is a useful starting point when the goal is to close the injection graph without forcing DTOs and method data into architectural layers. Add other sites only when those references are part of the boundary you want to enforce.

Consider a mapper method on an Application type:

```csharp
// OrderService is in the Application layer.
public class OrderService(IOrderRepository repository)
{
    public OrderDto Map(OrderRecord record, OrderStatus status)
    {
        return new OrderDto { Id = record.Id, Status = status.ToString() };
    }
}
```

With `requireRecognizedDependencies="Constructor"`, only `IOrderRepository` must be classified. With `requireRecognizedDependencies="Constructor, Method, MethodReturn, New"`, `OrderRecord`, `OrderStatus`, and `OrderDto` must also belong to configured layers because they appear at selected sites. Whether that counts as enforcement or as paperwork depends entirely on whether those types are genuinely part of the boundary you are defending.

At the root, the setting is site-scoped for every layered caller. On a layer, it is site-scoped for callers in that layer and its descendants, which is useful when only one area of a legacy codebase is ready to require fully classified dependencies. Recognized dependencies still pass through normal type policies and layer-edge rules.

#### IDE code fixes

When the dependency really does belong to a known role, the IDE can classify it into an existing layer by adding an exact `<Class typeName="..."/>` matcher. When the site was enforced too aggressively, the IDE can also remove the current site from `requireRecognizedDependencies` either globally or for the current caller layer.
