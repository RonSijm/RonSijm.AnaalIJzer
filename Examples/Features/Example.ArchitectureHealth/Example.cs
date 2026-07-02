public sealed class OrderService(OrderRepository repository);

public sealed class OrderRepository(OrderService service);

public sealed class LooseHelper;
