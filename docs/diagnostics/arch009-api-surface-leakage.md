### ARCH009 - API surface leakage

ARCH009 means an externally visible declaration exposes a type rejected by the owning layer's `<ApiSurface>` policy.

```text
'CandyOrderingService' (layer Application) exposes 'LollyQueryable'
(layer RepositoryQuerySurface) at MethodReturn: the API surface policy
in layer 'Application' blocks layer '/RepositoryQuerySurface' at MethodReturn
```

The diagnostic is reported on the exposed type syntax. Its properties include the caller and exposed type/layer, canonical API `Site`, `ApiMemberName`, exact denial reason, and the originating configuration location.

Common fixes are:

- project the internal type to a contract before returning it;
- make the declaration non-public when it is an implementation detail;
- add the exposed type to the intended contract layer;
- deliberately adjust the `<ApiSurface>` policy or its site filter.

Adding an `<AllowedDependency>` is not an ARCH009 fix by itself. That edge permits internal use; it does not grant permission to publish the type as API.

**Example project:** [`Example.Arch009.ApiSurfaceLeakage`](../../Examples/Diagnostics/Example.Arch009.ApiSurfaceLeakage)
