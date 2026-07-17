## Q/A

### Why are `Task` or `Nullable` blocked?

If you see a message like this:

```text
'ISsoManager' (layer Application/Contracts) may not depend on 'Task' (layer Crosscutting):
no allowed dependency gate from 'Application/Contracts' to 'Crosscutting' is configured in boundary 'Application'
```

then `Task` or `Nullable` has been classified into one of your configured layers. The analyzer does not treat framework types as forbidden by default. Once a matcher puts `Task`, `Nullable`, or another framework type in `Crosscutting`, normal layer and nested-boundary rules apply to it.

The cleanest fix is usually: do not classify framework types into application architecture layers unless you really mean to. Keep `Crosscutting` scoped to your own code:

```xml
<Layer name="Crosscutting">
  <Assembly exactName="MyCompany.Shared" />
  <Namespace startsWith="MyCompany.Shared" />
</Layer>
```

If an existing matcher is broad enough to catch `System.Threading.Tasks.Task`, `System.Nullable<T>`, or other platform types, narrow that matcher first. Use `<Exceptions>` only as a migration aid when narrowing the matcher is not immediately practical.

If you intentionally model framework or shared primitives as a layer, make that intention explicit. A separate `Framework` layer often reads better than mixing platform types into business crosscutting concerns:

```xml
<Layer name="Framework">
  <Class typeName="Task" />
  <Class typeName="Nullable" />
  <Class typeName="CancellationToken" />
</Layer>

<AllowedDependency from="*" to="Framework" appliesToDescendants="true" />
```

With nested layers, a top-level wildcard without `appliesToDescendants` is not enough. A type in `Application/Contracts` must also pass the `Application` boundary gate. Use `appliesToDescendants="true"` when the whitelist should be global-ish:

```xml
<AllowedDependency from="*" to="Crosscutting" appliesToDescendants="true" />
```

For stricter business boundaries, keep the edge local to the parent boundary instead:

```xml
<AllowedDependency from="*" to="Crosscutting" />

<Layer name="Application">
  <Layer name="Contracts">
    <Class endsWith="Manager" typeKind="Interface" />
  </Layer>

  <AllowedDependency from="Contracts" to="/Crosscutting" />
</Layer>
```

Use a site filter if the framework type should only appear in API shapes:

```xml
<AllowedDependency from="Contracts"
                   to="/Crosscutting"
                   allowedSites="MethodReturn, Property" />
```

If the diagnostic is ARCH001, the problem is a missing layer relationship. If the diagnostic is ARCH003, the type matched `<Forbidden>` or failed `<Allowed>`; fix the type policy instead.

---
