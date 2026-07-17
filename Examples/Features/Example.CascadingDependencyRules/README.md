# Example.CascadingDependencyRules

This example shows a framework-style whitelist for nested layers.

`Application/Contracts` and `DataAbstraction/Contracts` both expose framework primitives such as `Task`, `Nullable<T>` and `CancellationToken`.

The root rule:

```xml
<AllowedDependency from="*" to="Framework" appliesToDescendants="true" />
```

satisfies the root boundary and the nested parent boundary gates, so the project builds without repeating local `* -> /Framework` rules inside `Application` and `DataAbstraction`.
