// ReSharper disable All - Justification: Example File

using Microsoft.EntityFrameworkCore;

namespace Example.EntityFrameworkCore.ContextCreation;

public sealed class PizzaOrderingDbContext : DbContext { }

public sealed class PizzaOrderingDbContextFactory
{
    public PizzaOrderingDbContext Create() => new();
}

public sealed class PizzaOrderRepository(PizzaOrderingDbContext context) { }

public sealed class PizzaOrderApplicationService(PizzaOrderRepository repository)
{
    public void CreateContextOutsideTheFactory()
    {
        // ARCH001: Application -> Context is not allowed at Site=New.
        _ = new PizzaOrderingDbContext();
    }
}
