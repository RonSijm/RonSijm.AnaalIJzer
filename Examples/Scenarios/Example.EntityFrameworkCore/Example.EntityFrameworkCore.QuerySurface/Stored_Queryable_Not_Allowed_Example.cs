// ReSharper disable All - Justification: Example File

using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Example.EntityFrameworkCore.QuerySurface;

public sealed class PizzaOrderingDbContext : DbContext
{
    public DbSet<PizzaOrder> PizzaOrders => Set<PizzaOrder>();
}

public sealed class PizzaOrderRepository(PizzaOrderingDbContext context)
{
    public IQueryable<PizzaOrder> QueryPizzaOrders()
    {
        var result = context.PizzaOrders;

        return result;
    }
}

public sealed class PizzaOrderApplicationService(PizzaOrderRepository repository)
{
    public PizzaOrderProjection GetPizzaOrder(int pizzaOrderId)
    {
        var result = repository.QueryPizzaOrders()
            .Where(order => order.Id == pizzaOrderId)
            .Select(order => new PizzaOrderProjection(order.Id, order.Name))
            .Single();

        return result;
    }

    public PizzaOrderProjection GetPizzaOrderAfterKeepingTheQuery(int pizzaOrderId)
    {
        // ARCH_DEP_001: Application -> QuerySurface is not allowed at Site=Local.
        IQueryable<PizzaOrder> query = repository.QueryPizzaOrders();
        var result = query
            .Where(order => order.Id == pizzaOrderId)
            .Select(order => new PizzaOrderProjection(order.Id, order.Name))
            .Single();

        return result;
    }
}

public sealed record PizzaOrder(int Id, string Name);
public sealed record PizzaOrderProjection(int PizzaOrderId, string PizzaName);
