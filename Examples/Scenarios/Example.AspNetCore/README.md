# ASP.NET Core Example Pack

This pack uses real `Microsoft.NET.Sdk.Web` projects and real ASP.NET Core attributes such as `[ApiController]`, `[HttpGet]`, `[HttpPost]`, and `[FromRoute]`. It demonstrates rules that AnaalIjzer already understands through its framework-neutral configuration model; the analyzer does not reference ASP.NET Core and this pack does not add an ASP-specific plugin.

| Project | Existing AnaalIjzer capability | Intended result |
| --- | --- | --- |
| [`Example.AspNetCore.LayerBoundaries`](Example.AspNetCore.LayerBoundaries) | Classify controllers by `[ApiController]` and allow `Endpoint -> Application -> Persistence`, but not `Endpoint -> Persistence`. | One `ARCH001` for a controller that injects a repository directly. |
| [`Example.AspNetCore.ModelBindingNames`](Example.AspNetCore.ModelBindingNames) | Apply `RequireDeclarationNameMatchesType` to honest-type action parameters. | Two `ARCH008` findings for swapped `DoctorId patientId` / `PatientId doctorId` names. |
| [`Example.AspNetCore.OperationContracts`](Example.AspNetCore.OperationContracts) | Require selected controller actions to directly call a configured application owner with matching request and response shapes. | One `ARCH023` for an action that returns a response without delegating. |
| [`Example.AspNetCore.ApiSurface`](Example.AspNetCore.ApiSurface) | Keep a controller's public API limited to contracts even when the code may otherwise use a query surface. | One `ARCH009` for leaking `IQueryable<PizzaResponse>`. |

The pack deliberately does **not** claim that AnaalIjzer understands ASP.NET routing conventions. It does not compare `"{patientId}"` route tokens with parameters, discover minimal API handlers from `MapGet`, inventory endpoints across route groups, or inspect generated OpenAPI. Those are distinct framework-specific concerns. The examples show the useful source facts that are already explicit and mechanically provable today.
