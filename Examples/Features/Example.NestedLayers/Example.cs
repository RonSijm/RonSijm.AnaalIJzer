// ReSharper disable All - Justification: Example File
namespace Example.NestedLayers;

public class InvoiceContract { }

public class OrderRepository { }

// Allowed inside Ordering: Application -> Repository.
// Allowed across boundaries: Ordering -> Billing, Application egress, then Contracts ingress.
public class PlaceOrderService(OrderRepository repository, InvoiceContract invoice) { }

// A type that matches the parent scope but no child belongs directly to Ordering.
public class OrderModule(OrderRepository repository) { }

public class ShippingContract { }

// ARCH001: the root Catalog -> Shipping gate is missing.
public class CatalogService(ShippingContract contract) { }

public class SupportContract { }

// ARCH001: Fulfillment/Application has no egress gate to Support/Contracts.
public class FulfillmentService(SupportContract contract) { }

public class PaymentContract { }

// ARCH001: Payments has no ingress gate into Contracts.
public class InventoryService(PaymentContract contract) { }