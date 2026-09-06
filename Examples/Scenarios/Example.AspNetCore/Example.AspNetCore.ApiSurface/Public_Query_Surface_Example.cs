// ReSharper disable All - Justification: Example File

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Example.AspNetCore.ApiSurface;

[ApiController]
[Route("api/pizzas")]
public sealed class PizzaCatalogController(PizzaCatalogService service) : ControllerBase
{
    [HttpGet("{pizzaId}")]
    public PizzaResponse GetPizza(int pizzaId)
    {
        var result = service.GetPizza(pizzaId);

        return result;
    }

    // ARCH009: code may use IQueryable internally, but an HTTP endpoint must not publish it as its API.
    [HttpGet]
    public IQueryable<PizzaResponse> GetRawPizzas()
    {
        var result = service.GetRawPizzas();

        return result;
    }
}

public sealed class PizzaCatalogService
{
    public PizzaResponse GetPizza(int pizzaId)
    {
        var result = new PizzaResponse(pizzaId, "Margherita");

        return result;
    }

    public IQueryable<PizzaResponse> GetRawPizzas()
    {
        var result = Array.Empty<PizzaResponse>().AsQueryable();

        return result;
    }
}

public sealed record PizzaResponse(int PizzaId, string PizzaName);
