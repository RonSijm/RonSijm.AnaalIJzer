// A repository may own a short-lived query surface without allowing application
// or presentation types to retain that surface as a dependency.

// Presentation -> Application is allowed.
public sealed class OrderEndpoint(OrderService orderService)
{
	public OrderProjection GetOrder() => orderService.GetOrder();
}

// Application -> Persistence is allowed.
public sealed class OrderService(OrderRepository repository)
{
	public OrderProjection GetOrder() => repository.QueryOrders().ForCurrentCustomer().Project();

	// ARCH001: Application -> QuerySurface is not allowed at Site=Local.
	public OrderProjection GetOrderThroughLocalQuery()
	{
		OrderQuery query = repository.QueryOrders();
		OrderProjection projection = query.Project();

		return projection;
	}
}

// Persistence -> QuerySurface is allowed.
public sealed class OrderRepository
{
	public OrderQuery QueryOrders() => new();
}

// QuerySurface -> Projection is allowed.
public sealed class OrderQuery
{
	public OrderQuery ForCurrentCustomer() => this;

	public OrderProjection Project() => new(42);
}

public sealed record OrderProjection(int OrderId);

// ARCH001: Application -> QuerySurface is not allowed at Site=Constructor.
public sealed class OrderDashboardService(OrderQuery query)
{
	public OrderProjection PreviewOrder() => query.Project();
}
