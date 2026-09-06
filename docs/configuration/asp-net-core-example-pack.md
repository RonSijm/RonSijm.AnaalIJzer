## ASP.NET Core example pack

AnaalIjzer does not need an ASP.NET Core dependency to enforce many useful Web API rules. Roslyn resolves the symbols in your application; the ordinary matcher and policy vocabulary can then select facts such as `[ApiController]`, `ControllerBase`, action parameters, public return types, and direct method calls.

The runnable [`Example.AspNetCore`](../../Examples/Scenarios/Example.AspNetCore) pack uses real `Microsoft.NET.Sdk.Web` projects to show four framework-neutral rules:

| Project | Rule demonstrated | Intended finding |
| --- | --- | --- |
| [`LayerBoundaries`](../../Examples/Scenarios/Example.AspNetCore/Example.AspNetCore.LayerBoundaries) | `<Class withAttribute="ApiController" />` classifies endpoints, then ordinary layer edges keep controllers from injecting repositories directly. | `ARCH001` |
| [`ModelBindingNames`](../../Examples/Scenarios/Example.AspNetCore/Example.AspNetCore.ModelBindingNames) | `RequireDeclarationNameMatchesType` protects selected honest-type action parameters from misleading names. | `ARCH008` |
| [`OperationContracts`](../../Examples/Scenarios/Example.AspNetCore/Example.AspNetCore.OperationContracts) | An explicit `<Operations>` rule requires selected controller actions to directly invoke one application owner with a selected request and response shape. | `ARCH023` |
| [`ApiSurface`](../../Examples/Scenarios/Example.AspNetCore/Example.AspNetCore.ApiSurface) | `<ApiSurface>` prevents an endpoint from exposing `IQueryable<T>` even when ordinary dependency rules permit use of it. | `ARCH009` |

For example, a controller layer can be classified solely from its real attribute:

```xml
<Layer name="Endpoint">
  <Class withAttribute="ApiController" />
</Layer>

<Layer name="Application">
  <Class endsWith="Service" />
</Layer>

<AllowedDependency from="Endpoint" to="Application" />
```

These rules examine explicit source semantics. They do **not** infer route templates, compare a `"{patientId}"` token to a parameter, discover minimal-API handlers from `MapGet`, inventory route groups, or compare source with generated OpenAPI. Those are genuinely ASP.NET-specific concerns and are deliberately outside this generic pack.
