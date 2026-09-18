// ReSharper disable All - Justification: Example File

using Microsoft.AspNetCore.Mvc;

namespace Example.AspNetCore.LayerBoundaries;

[ApiController]
[Route("api/pizza-orders")]
public sealed class PizzaOrdersController(PizzaOrderingService service, PizzaOrderRepository repository) : ControllerBase
{
    [HttpGet("{pizzaOrderId}")]
    public PizzaOrderResponse GetPizzaOrder(PizzaOrderId pizzaOrderId)
    {
        var response = service.GetPizzaOrder(pizzaOrderId);

        return response;
    }

    // ARCH_DEP_001: a controller is an endpoint, so it must not inject a repository directly.
    // The repository parameter above is intentionally present to make that boundary visible.
}

public sealed class PizzaOrderingService(PizzaOrderRepository repository)
{
    public PizzaOrderResponse GetPizzaOrder(PizzaOrderId pizzaOrderId)
    {
        var result = repository.GetPizzaOrder(pizzaOrderId);

        return result;
    }
}

public sealed class PizzaOrderRepository
{
    public PizzaOrderResponse GetPizzaOrder(PizzaOrderId pizzaOrderId)
    {
        var result = new PizzaOrderResponse(pizzaOrderId.Value, "Preparing");

        return result;
    }
}

public readonly record struct PizzaOrderId(int Value);
public sealed record PizzaOrderResponse(int PizzaOrderId, string Status);
