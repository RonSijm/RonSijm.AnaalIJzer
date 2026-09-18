// ReSharper disable All - Justification: Example File

using Microsoft.EntityFrameworkCore;

namespace Example.EntityFrameworkCore.ContextBoundary;

public sealed class PizzaOrderApplicationService(PizzaOrderRepository repository, PizzaOrderingDbContext context)
{
    public PizzaOrderProjection GetPizzaOrder(int pizzaOrderId)
    {
        var result = repository.GetPizzaOrder(pizzaOrderId);

        return result;
    }

    // ARCH_DEP_001: Application -> Context is not allowed at Site=Constructor.
    // The service should ask the repository, rather than receive DbContext directly.
}

public sealed class PizzaOrderRepository(PizzaOrderingDbContext context)
{
    public PizzaOrderProjection GetPizzaOrder(int pizzaOrderId)
    {
        var result = new PizzaOrderProjection(pizzaOrderId, "Queued");

        return result;
    }
}

public sealed class PizzaOrderingDbContext : DbContext { }
public sealed record PizzaOrderProjection(int PizzaOrderId, string Status);
