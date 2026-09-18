// ReSharper disable All - Justification: Example File

using Microsoft.AspNetCore.Mvc;

namespace Example.AspNetCore.OperationContracts;

[ApiController]
[Route("api/pizza-orders")]
public sealed class PizzaOrdersController(PizzaOrderingService service) : ControllerBase
{
    [HttpPost]
    public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
    {
        var result = service.PlacePizzaOrder(request);

        return result;
    }
}

[ApiController]
[Route("api/pizza-order-preview")]
public sealed class PizzaOrderPreviewController : ControllerBase
{
    // ARCH_OPCT_002: the selected HTTP action returns the configured response, but it never delegates to the owner.
    [HttpPost]
    public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
    {
        var result = new PlacePizzaOrderResponse(request.PizzaName, "Preview only");

        return result;
    }
}

public sealed class PizzaOrderingService
{
    public PlacePizzaOrderResponse PlacePizzaOrder(PlacePizzaOrderRequest request)
    {
        var result = new PlacePizzaOrderResponse(request.PizzaName, "Accepted");

        return result;
    }
}

public sealed record PlacePizzaOrderRequest(string PizzaName);
public sealed record PlacePizzaOrderResponse(string PizzaName, string Status);
