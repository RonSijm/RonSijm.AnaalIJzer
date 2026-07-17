// ReSharper disable All - Justification: Example File
namespace Example.ArchitectureHealth;

public sealed class OrderService(OrderRepository repository);

public sealed class OrderRepository(OrderService service);

public sealed class LooseHelper;