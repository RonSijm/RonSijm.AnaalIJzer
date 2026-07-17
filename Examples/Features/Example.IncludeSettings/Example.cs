// ReSharper disable All - Justification: Example File
// ARCH001: AdminEndpoint skips the Application layer and talks directly
// to Persistence. The layer definitions come from SharedApplicationLayers.anl.

namespace Example.IncludeSettings;

public interface IOrderRepository { }

// Presentation -> Application is allowed by the project settings.
public interface IOrderService { }
public class OrderEndpoint(IOrderService service) { }

// Application -> Persistence is allowed by the included shared settings.
public class OrderService(IOrderRepository repository) { }

// ARCH001: Presentation -> Persistence has no AllowedDependency edge.
public class AdminEndpoint(IOrderRepository repository) { }